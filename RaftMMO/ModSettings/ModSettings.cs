
using System.Collections.Generic;
using System.Linq;

namespace RaftMMO.ModSettings
{
    public class ModSettings
    {
        [System.Serializable()]
        public class SerializableModSettings
        {
            public RaftEntry[] MetRafts = new RaftEntry[0];
            public PlayerEntry[] MetPlayers = new PlayerEntry[0];
            public int LogLevel = (int)RaftMMO.ModSettings.LogLevel.WARNING;
            public bool LogVerbose = false;
            public bool OnlyMeetSteamFriends = false;
            public bool EnableBuoySmoke = true;
            public long GlobalMeetCoolDown = 1 * 60 * 1000;
            public long IndividualMeetCoolDown = 15 * 60 * 1000;
        }

        public SerializableModSettings Serialize()
        {
            var serialized = new SerializableModSettings();
            serialized.MetRafts = MetRafts.Values.ToArray();
            serialized.MetPlayers = MetPlayers.Values.ToArray();
            serialized.LogLevel = (int)LogLevel;
            serialized.LogVerbose = LogVerbose;
            serialized.OnlyMeetSteamFriends = OnlyMeetSteamFriends;
            serialized.EnableBuoySmoke = EnableBuoySmoke;
            serialized.GlobalMeetCoolDown = GlobalMeetCoolDown;
            serialized.IndividualMeetCoolDown = IndividualMeetCoolDown;
            return serialized;
        }

        public void Deserialize(SerializableModSettings serialized)
        {
            MetRafts.Clear();
            MetPlayers.Clear();
            foreach (var raft in serialized.MetRafts)
            {
                MetRafts.Add(SettingsManager.MakeUniqueID(raft.steamID, raft.sessionID), raft);
            }
            foreach (var player in serialized.MetPlayers)
            {
                MetPlayers.Add(player.steamID, player);
            }
            LogLevel = (LogLevel)serialized.LogLevel;
            LogVerbose = serialized.LogVerbose;
            OnlyMeetSteamFriends = serialized.OnlyMeetSteamFriends;
            EnableBuoySmoke = serialized.EnableBuoySmoke;
            GlobalMeetCoolDown = serialized.GlobalMeetCoolDown;
            IndividualMeetCoolDown = serialized.IndividualMeetCoolDown;
        }

        public Dictionary<string, RaftEntry> MetRafts = new Dictionary<string, RaftEntry>();

        public Dictionary<ulong, PlayerEntry> MetPlayers = new Dictionary<ulong, PlayerEntry>();

        public LogLevel LogLevel = LogLevel.WARNING;
        public bool LogVerbose = false;

        public bool OnlyMeetSteamFriends = false;
        public bool EnableBuoySmoke = true;

        public long GlobalMeetCoolDown = 1 * 30 * 1000;
        public long IndividualMeetCoolDown = 10 * 60 * 1000;
    }
}
