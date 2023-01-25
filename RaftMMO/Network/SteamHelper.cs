using RaftMMO.Utilities;
using Steamworks;
using System.Collections.Generic;

namespace RaftMMO.Network
{
    public class SteamHelper
    {
        private static HashSet<CSteamID> connectedSteamIDs = new HashSet<CSteamID>();

        public static void CloseAll()
        {
            foreach (var steamID in connectedSteamIDs)
            {
                SteamNetworking.CloseP2PChannelWithUser(steamID, Globals.SteamNetworkChannel);
            }
            connectedSteamIDs.Clear();
        }

        public static bool Connect(CSteamID steamID)
        {
            connectedSteamIDs.Add(steamID);
            return SteamNetworking.AcceptP2PSessionWithUser(steamID);
        }

        public static bool Close(CSteamID steamID)
        {
            connectedSteamIDs.Remove(steamID);
            return SteamNetworking.CloseP2PChannelWithUser(steamID, Globals.SteamNetworkChannel);
        }

        public static void OpenSteamChat(CSteamID steamID)
        {
            SteamFriends.ActivateGameOverlayToUser("chat", steamID);
        }

        public static void AddSteamFriend(CSteamID steamID)
        {
            switch (SteamFriends.GetFriendRelationship(steamID))
            {
                case EFriendRelationship.k_EFriendRelationshipNone:
                case EFriendRelationship.k_EFriendRelationshipBlocked:  // Blocked is not blocked, see https://github.com/maxvollmer/ISteamFriends-EFriendRelationship
                    SteamFriends.ActivateGameOverlayToUser("friendadd", steamID);
                    break;

                case EFriendRelationship.k_EFriendRelationshipRequestRecipient:
                    SteamFriends.ActivateGameOverlayToUser("friendrequestaccept", steamID);
                    break;
            }
        }

        public static bool IsSameSteamID(ulong steamID1, ulong steamID2)
        {
            if (steamID1 == steamID2)
                return true;

            if (Globals.TEMPDEBUGConnectToLocalPlayer)
            {
                return steamID1 == steamID2 + 1 || steamID2 == steamID1 + 1;
            }

            return false;
        }

        public static bool IsSameSteamID(CSteamID steamID1, CSteamID steamID2)
        {
            return IsSameSteamID(steamID1.m_SteamID, steamID2.m_SteamID);
        }

        public static string GetSteamUserName(CSteamID steamID, bool isCharacterTag)
        {
            bool isHost = false;
            if (Raft_Network.IsHost && RemoteSession.IsConnectedToPlayer)
                isHost = steamID.m_SteamID == RemoteSession.ConnectedPlayer.m_SteamID;
            else if (!Raft_Network.IsHost && ClientSession.IsHostConnectedToPlayer)
                isHost = steamID.m_SteamID == ClientSession.ConnectedSteamID;

            bool isFriend = SteamFriends.GetFriendRelationship(steamID) == EFriendRelationship.k_EFriendRelationshipFriend;

            if (isCharacterTag)
            {
                var name = "[RaftMMO Remote " + (isHost ? "Host" : "Player") +"]";

                if (GeneralSettingsBox.ShowNameTag && isFriend)
                {
                    name += "\n" + SteamFriends.GetFriendPersonaName(steamID);
                }

                return name;
            }
            else if (isFriend)
            {
                return SteamFriends.GetFriendPersonaName(steamID);
            }
            else
            {
                return "Player";
            }
        }

        public static string GetSteamIDDisplayString(CSteamID cSteamID)
        {
            if (cSteamID.m_SteamID == 0)
                return "****";

            string steamid = "" + cSteamID.m_SteamID;
            if (steamid.Length <= 4)
                return "****";

            return new string('*', steamid.Length - 4) + steamid.Substring(steamid.Length - 4);
        }
    }
}
