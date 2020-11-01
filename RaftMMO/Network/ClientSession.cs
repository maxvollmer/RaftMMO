using HarmonyLib;
using RaftMMO.ModSettings;
using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

using Object = UnityEngine.Object;

namespace RaftMMO.Network
{
    public class ClientSession
    {
        public static bool IsHostConnectedToPlayer { get; private set; } = false;
        public static ulong ConnectedSteamID { get; private set; } = 0;
        public static string ConnectedSessionID { get; private set; } = null;

        private class RaftUpdateStuff
        {
            public Stopwatch stopwatch = new Stopwatch();
            public int counter = 0;
            public bool full = true;
        }

        private static Dictionary<ulong, Stopwatch> PosUpdateSessionPlayerCache { get; set; } = new Dictionary<ulong, Stopwatch>();
        private static Dictionary<ulong, RaftUpdateStuff> RaftUpdateSessionPlayerCache { get; set; } = new Dictionary<ulong, RaftUpdateStuff>();
        private static Dictionary<ulong, bool> ConnectionSentPlayerCache { get; set; } = new Dictionary<ulong, bool>();

        public static void Update()
        {
            if (Semih_Network.IsHost)
            {
                List<ulong> steamIDs = new List<ulong>();

                foreach (var player in GetSessionPlayers())
                {
                    var steamID = player.steamID.m_SteamID;

                    if (!ConnectionSentPlayerCache.ContainsKey(steamID))
                    {
                        ConnectionSentPlayerCache.Add(steamID, !RemoteSession.IsConnectedToPlayer);
                    }
                    if (RemoteSession.IsConnectedToPlayer != ConnectionSentPlayerCache[steamID])
                    {
                        MessageManager.SendConnectedMessage(player.steamID);
                        ConnectionSentPlayerCache[steamID] = RemoteSession.IsConnectedToPlayer;
                    }

                    if (!RaftUpdateSessionPlayerCache.ContainsKey(steamID)
                        || !RaftUpdateSessionPlayerCache[steamID].stopwatch.IsRunning
                        || RaftUpdateSessionPlayerCache[steamID].stopwatch.ElapsedMilliseconds >= Globals.RaftUpdateFrequency)
                    {
                        if (!RaftUpdateSessionPlayerCache.ContainsKey(steamID))
                            RaftUpdateSessionPlayerCache.Add(steamID, new RaftUpdateStuff());

                        MessageManager.UploadBuoys(player.steamID);
                        RaftUpdateSessionPlayerCache[steamID].counter = MessageManager.UploadRemoteRaft(player.steamID, RaftUpdateSessionPlayerCache[steamID].full, RaftUpdateSessionPlayerCache[steamID].counter);
                        MessageManager.UploadListOfPlayers(player.steamID, RemoteRaft.GetRemotePlayers().Select(p => p.steamID.m_SteamID).ToArray());

                        RaftUpdateSessionPlayerCache[steamID].full = false;
                        RaftUpdateSessionPlayerCache[steamID].stopwatch.Restart();
                    }

                    if (!PosUpdateSessionPlayerCache.ContainsKey(steamID)
                        || !PosUpdateSessionPlayerCache[steamID].IsRunning
                        || PosUpdateSessionPlayerCache[steamID].ElapsedMilliseconds >= Globals.PositionUpdateFrequency)
                    {
                        MessageManager.UploadRemotePosUpdates(player.steamID);

                        if (!PosUpdateSessionPlayerCache.ContainsKey(steamID))
                            PosUpdateSessionPlayerCache.Add(steamID, new Stopwatch());

                        PosUpdateSessionPlayerCache[steamID].Restart();
                    }
                }

                PosUpdateSessionPlayerCache = PosUpdateSessionPlayerCache.Where(pair => steamIDs.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            else
            {
                Globals.RemotePosRotation = 0f;
                MessageManager.ReceiveRaftMMOMessages();
                if (!IsHostConnectedToPlayer)
                {
                    RemoteRaft.SetLastUpdateCounter(-1);
                }
            }
        }

        public static void Disconnect()
        {
            IsHostConnectedToPlayer = false;
            PosUpdateSessionPlayerCache.Clear();
            RaftUpdateSessionPlayerCache.Clear();
        }

        public static IEnumerable<Network_Player> GetSessionPlayers()
        {
            return ComponentManager<Semih_Network>.Value.remoteUsers.Where(u => u.Key.IsValid() && !u.Value.IsLocalPlayer).Select(u => u.Value);
        }

        public static void HandleConnectedMessage(Messages.ConnectedMessage message)
        {
            IsHostConnectedToPlayer = message.IsConnectedToPlayer;
            ConnectedSteamID = message.ConnectedSessionSteamID;
            ConnectedSessionID = message.ConnectedSessionID;
        }

        public static void HandleBuoyUpdate(Messages.BuoysUpdateMessage message)
        {
            Globals.CurrentRaftMeetingPoint = message.CurrentRaftMeetingPoint;
            Globals.CurrentRaftMeetingPointDistance = message.CurrentRaftMeetingPointDistance;
            BuoyManager.ReceiveBuyLocationsFromHost(message.BuoyLocations);
        }

        public static void HandleDisconnect()
        {
            Disconnect();
        }

        public static void HandleRaftPositionUpdate(Messages.IPositionUpdateMessage message)
        {
            RemoteRaft.MoveTo(message.Position.Vector3, message.Rotation.Quaternion);
        }

        public static void HandlePlayerPositionUpdate(Messages.PlayerUpdateMessage message)
        {
            if (SettingsManager.IsBlockedPlayer(message.steamID))
            {
                RemoteRaft.RemoveRemotePlayer(message.steamID);
                return;
            }

            var translatedPos = Globals.CurrentRaftMeetingPoint + message.position.Vector3;

            var player = RemoteRaft.GetRemotePlayer(message.steamID, message.model, translatedPos);
            if (player == null)
                return;

            player.PersonController.SetNetworkProperties(message.playerUpdate);
            player.Animator.SetNetworkProperties(message.playerUpdate);

            GameObject relativePositionFaker = new GameObject();
            relativePositionFaker.transform.position = translatedPos;

            switch (message.raftAttachStatus)
            {
                case RaftAttachStatus.ATTACHED_TO_OWN_RAFT:
                    player.transform.SetParentSafe(RemoteRaft.Transform);
                    break;
                case RaftAttachStatus.ATTACHED_TO_REMOTE_RAFT:
                    player.transform.SetParentSafe(SingletonGeneric<GameManager>.Singleton.lockedPivot);
                    relativePositionFaker.transform.SetParent(SingletonGeneric<GameManager>.Singleton.lockedPivot, true);
                    break;
                case RaftAttachStatus.NOT_ATTACHED:
                    player.transform.SetParentSafe(null);
                    break;
            }

            Traverse.Create(player.PersonController).Field("networkPosition").SetValue(relativePositionFaker.transform.localPosition);
            Traverse.Create(player.PersonController).Field("networkRotationX").SetValue(message.RotationX);
            Traverse.Create(player.PersonController).Field("networkRotationY").SetValue(message.RotationY + Globals.RemotePosRotation);

            relativePositionFaker.transform.SetParent(null);
            Object.Destroy(relativePositionFaker);
        }

        public static void HandleFullRaftUpdateRequest(CSteamID steamID)
        {
            // clearing the steam id will make us send a full raft update next time for this client
            PosUpdateSessionPlayerCache.Remove(steamID.m_SteamID);
            if (RaftUpdateSessionPlayerCache.TryGetValue(steamID.m_SteamID, out RaftUpdateStuff stuff))
            {
                stuff.full = true;
            }
        }
    }
}
