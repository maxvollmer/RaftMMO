using RaftMMO.Network;
using System.Collections.Generic;
using System.Linq;
using RaftMMO.Network.Messages;
using SerializableData = RaftMMO.Network.SerializableData;
using RaftMMO.Utilities;
using RaftMMO.ModSettings;

namespace RaftMMO.RaftCopyTools
{
    public class RaftDataManager
    {
        private HashSet<SerializableData.RaftBlockData> cachedBlocks = new HashSet<SerializableData.RaftBlockData>();

        public static RaftDataManager Remote { get; private set; } = new RaftDataManager();

        private static Dictionary<ulong, RaftDataManager> Clients { get; set; } = new Dictionary<ulong, RaftDataManager>();

        public static RaftDataManager Client(ulong steamID)
        {
            if (!Clients.ContainsKey(steamID))
            {
                Clients.Add(steamID, new RaftDataManager());
            }
            return Clients[steamID];
        }

        public RaftDeltaMessage CreateRaftDeltaMessage(SerializableData.RaftData raftData, bool fullMsg, bool worldpos)
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("CreateRaftRGDMessage, fullMsg: " + fullMsg);
            }

            var raftBlockData = new HashSet<SerializableData.RaftBlockData>();
            raftBlockData.UnionWith(raftData.blockData);

            List<SerializableData.RaftBlockData> addedBlocks = new List<SerializableData.RaftBlockData>();
            List<SerializableData.RaftBlockData> removedBlocks = new List<SerializableData.RaftBlockData>();

            RaftDeltaMessage msg;

            if (fullMsg)
            {
                var raft = ComponentManager<Raft>.Value;
                var pos = raft.transform.position;
                if (!worldpos) pos -= Globals.CurrentRaftMeetingPoint;
                msg = new FullRaftMessage(
                    pos,
                    raft.transform.rotation,
                    Globals.RemotePosRotation);

                addedBlocks.AddRange(raftBlockData);
            }
            else
            {
                msg = new RaftDeltaMessage();

                addedBlocks.AddRange(raftBlockData.Where(block => !cachedBlocks.Contains(block)));
                removedBlocks.AddRange(cachedBlocks.Where(cachedBlock => !raftBlockData.Contains(cachedBlock)));
            }

            msg.added_data = new SerializableData.RaftData(addedBlocks.ToArray());
            msg.removed_data = new SerializableData.RaftData(removedBlocks.ToArray());

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("RaftDataManager.CreateRaftDeltaMessage Done:\n"
                    + "raftBlockData: " + GameObjectDebugger.DebugPrint(raftBlockData.ToArray()) + "\n"
                    + "cachedBlocks: " + GameObjectDebugger.DebugPrint(cachedBlocks.ToArray()) + "\n"
                    + "addedBlocks: " + GameObjectDebugger.DebugPrint(addedBlocks.ToArray()) + "\n"
                    + "removedBlocks: " + GameObjectDebugger.DebugPrint(removedBlocks.ToArray()));
            }

            cachedBlocks = raftBlockData;

            return msg;
        }

        public static void Update()
        {
            if (Raft_Network.IsHost)
            {
                var steamIDs = ClientSession.GetSessionPlayers().Select(p => p.steamID.m_SteamID);
                Clients = Clients.Where(c => steamIDs.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            }
        }

        public static void Clear()
        {
            Remote = new RaftDataManager();
            Clients.Clear();
        }
    }
}
