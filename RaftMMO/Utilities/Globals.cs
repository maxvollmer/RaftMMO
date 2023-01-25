using UnityEngine;

namespace RaftMMO.Utilities
{
    public class Globals
    {
        public static int ModNetworkVersion { get; } = 20;
        public static int SteamNetworkChannel { get; } = 1339;

        public static int ReliableMessageSizeLimit { get; } = 1000000;


        public static long ConnectToLobbyTimeout { get; } = 5000;
        public static long ConnectToPlayerTimeout { get; } = 10000;
        public static long SwitchLobbyCooldownTimeout { get; } = 1000;

        public static long PositionUpdateFrequency { get; } = 40;   // 25fps
        public static long RaftUpdateFrequency { get; } = 1000;     // once a second


        public static bool IsDisabled { get; set; } = true;

        public static AssetBundle AssetBundle { get; set; } = null;

        public static Vector3 CurrentRaftMeetingPoint { get; set; } = new Vector3(9999999999999f, -9999999999999f, 9999999999999f);
        public static float CurrentRaftMeetingPointDistance { get; set; } = 9999999999999f;
        public static Vector3 CurrentPushAwayOffset { get; set; } = Vector3.zero;

        public static float RemotePosRotation { get; set; } = 360f;

        public static bool FullRaftUpdateRequested { get; set; } = true;


        public static System.Random RND { get; } = new System.Random();


        public static string LobbyAllName = "de.maxvollmer.raftmmo.all";
        public static string LobbyConnectName = "de.maxvollmer.raftmmo.lobby";



        /// TODO TEMP TODO TEMP
        /// TODO TEMP TODO TEMP
        /// TODO TEMP TODO TEMP
        public static bool TEMPDEBUGNoStartCoolDown { get; } = false;
        public static bool TEMPDEBUGConnectToLocalPlayer { get; } = false;
        public static bool TEMPDEBUGStaticBuoyPosition { get; } = false;

        /// TODO TEMP TODO TEMP
        /// TODO TEMP TODO TEMP
        /// TODO TEMP TODO TEMP
    }
}
