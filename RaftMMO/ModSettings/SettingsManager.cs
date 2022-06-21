using RaftMMO.MainMenu;
using RaftMMO.Network;
using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMMO.ModSettings
{
    public class SettingsManager
    {
        public static ModSettings Settings { get; set; } = new ModSettings();

        public static IEnumerable<RaftEntry> GetMetRaftsByDate
        {
            get
            {
                return Settings.MetRafts.Values.Where(raftEntry => !raftEntry.isBlocked && !raftEntry.isFavorite).OrderByDescending(raftEntry => raftEntry.lastMet);
            }
        }

        public static IEnumerable<RaftEntry> GetBlockedRaftsByDate
        {
            get
            {
                return Settings.MetRafts.Values.Where(raftEntry => raftEntry.isBlocked).OrderByDescending(raftEntry => raftEntry.lastMet);
            }
        }

        public static IEnumerable<RaftEntry> GetFavoritedRaftsByDate
        {
            get
            {
                return Settings.MetRafts.Values.Where(raftEntry => !raftEntry.isBlocked && raftEntry.isFavorite).OrderByDescending(raftEntry => raftEntry.lastMet);
            }
        }

        public static IEnumerable<PlayerEntry> GetMetPlayersByDate
        {
            get
            {
                return Settings.MetPlayers.Values.Where(playerEntry => !playerEntry.isBlocked).OrderByDescending(raftEntry => raftEntry.lastMet);
            }
        }

        public static IEnumerable<PlayerEntry> GetBlockedPlayersByDate
        {
            get
            {
                return Settings.MetPlayers.Values.Where(playerEntry => playerEntry.isBlocked).OrderByDescending(raftEntry => raftEntry.lastMet);
            }
        }

        public static bool IsBlockedPlayer(ulong steamID)
        {
            if (Settings.MetPlayers.TryGetValue(steamID, out PlayerEntry playerEntry) && playerEntry.isBlocked)
                return true;

            // Ignored means Blocked. See https://github.com/maxvollmer/ISteamFriends-EFriendRelationship
            if (SteamFriends.GetFriendRelationship(new CSteamID(steamID)) == EFriendRelationship.k_EFriendRelationshipIgnored)
                return true;

            return false;
        }

        public static bool IsBlockedRaft(ulong steamID, string sessionID)
        {
            if (Settings.MetRafts.TryGetValue(MakeUniqueID(steamID, sessionID), out RaftEntry raftEntry))
            {
                return raftEntry.isBlocked;
            }
            return IsBlockedPlayer(steamID);
        }

        public static bool IsFavoritedRaft(ulong steamID, string sessionID)
        {
            if (Settings.MetRafts.TryGetValue(MakeUniqueID(steamID, sessionID), out RaftEntry raftEntry))
            {
                return raftEntry.isFavorite && !raftEntry.isBlocked;
            }
            return false;
        }

        public static void BlockRaft(ulong steamID, string sessionID, bool unblock = false)
        {
            if (Settings.MetRafts.TryGetValue(MakeUniqueID(steamID, sessionID), out RaftEntry raftEntry))
            {
                raftEntry.isBlocked = !unblock;
                if (raftEntry.isBlocked)
                {
                    raftEntry.isFavorite = false;

                    if (Raft_Network.IsHost && RemoteSession.IsConnectedPlayer(new CSteamID(steamID)) && RemoteSession.ConnectedSessionID == sessionID)
                    {
                        RemoteSession.Disconnect();
                    }
                }

                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshRaftEntries();
            }
        }

        public static void FavoriteRaft(ulong steamID, string sessionID, bool unfavorite = false)
        {
            if (Settings.MetRafts.TryGetValue(MakeUniqueID(steamID, sessionID), out RaftEntry raftEntry))
            {
                raftEntry.isFavorite = !unfavorite;

                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshRaftEntries();
            }
        }

        public static void ForgetRaft(ulong steamID, string sessionID)
        {
            if (Settings.MetRafts.Remove(MakeUniqueID(steamID, sessionID)))
            {
                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshRaftEntries();

                if ((Raft_Network.IsHost && RemoteSession.IsConnectedPlayer(new CSteamID(steamID)) && RemoteSession.ConnectedSessionID == sessionID)
                    || (!Raft_Network.IsHost && SteamHelper.IsSameSteamID(ClientSession.ConnectedSteamID, steamID) && ClientSession.ConnectedSessionID == sessionID))
                {
                    AddMetRaft(steamID, sessionID, true);
                }
            }
        }

        public static void BlockPlayer(ulong steamID, bool unblock = false)
        {
            if (Settings.MetPlayers.TryGetValue(steamID, out PlayerEntry playerEntry))
            {
                playerEntry.isBlocked = !unblock;
                if (playerEntry.isBlocked)
                {
                    RemoteRaft.RemoveRemotePlayer(steamID);
                }

                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshPlayerEntries();
            }
        }

        public static void IncrementPlayerTradeCount(ulong steamID)
        {
            if (Settings.MetPlayers.TryGetValue(steamID, out PlayerEntry playerEntry))
            {
                playerEntry.tradedTimes++;
                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshPlayerEntries();
            }
            else if (Globals.TEMPDEBUGConnectToLocalPlayer && Settings.MetPlayers.TryGetValue(steamID - 1, out playerEntry))
            {
                playerEntry.tradedTimes++;
                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshPlayerEntries();
            }
        }

        public static void ForgetPlayer(ulong steamID)
        {
            if (Settings.MetPlayers.Remove(steamID))
            {
                SettingsSaver.IsDirty = true;
                SettingsMenuBuilder.RefreshRaftEntries();
                RemoteRaft.RemoveRemotePlayer(steamID);
            }
        }

        public static void AddMetPlayer(ulong steamID, int model)
        {
            if (Settings.MetPlayers.TryGetValue(steamID, out PlayerEntry playerEntry))
            {
                playerEntry.lastMet = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                playerEntry.metTimes++;
                playerEntry.model = model;
            }
            else
            {
                playerEntry = new PlayerEntry(
                    steamID,
                    model,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                );
                Settings.MetPlayers.Add(steamID, playerEntry);
            }

            var unique_id = GetCurrentRemoteSessionUniqueID();
            if (unique_id != null)
            {
                playerEntry.rafts.Add(unique_id);

                // update the number of players for the current raft entry
                if (Settings.MetRafts.TryGetValue(unique_id, out RaftEntry raftEntry))
                {
                    raftEntry.numPlayers = Math.Max(raftEntry.numPlayers, RemoteRaft.GetRemotePlayerCount());
                    SettingsMenuBuilder.RefreshRaftEntries();
                }
            }

            SettingsSaver.IsDirty = true;
            SettingsMenuBuilder.RefreshPlayerEntries();
        }

        public static void AddMetRaft(ulong steamID, string sessionID, bool takescreenshot)
        {
            string unique_id = MakeUniqueID(steamID, sessionID);

            if (Settings.MetRafts.TryGetValue(unique_id, out RaftEntry raftEntry))
            {
                raftEntry.numPlayers = Math.Max(raftEntry.numPlayers, RemoteRaft.GetRemotePlayerCount());
                raftEntry.lastMet = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                raftEntry.metTimes++;
            }
            else
            {
                raftEntry = new RaftEntry(
                    steamID,
                    RemoteSession.ConnectedSessionID,
                    RemoteRaft.GetRemotePlayerCount(),
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                );
                Settings.MetRafts.Add(unique_id, raftEntry);
            }

            if (takescreenshot)
            {
                RemoteRaftScreenshotTaker.TakeScreenshot(GetScreenshotPath(steamID, sessionID));
            }

            SettingsSaver.IsDirty = true;
            SettingsMenuBuilder.RefreshRaftEntries();
        }

        public static string GetScreenshotPath(ulong steamID, string sessionID)
        {
            return SettingsSaver.ScreenshotsPath + MakeUniqueID(steamID, sessionID) + ".png";
        }

        public static string MakeUniqueID(ulong steamID, string sessionID)
        {
            return steamID + "_" + sessionID;
        }

        private static string GetCurrentRemoteSessionUniqueID()
        {
            if (Raft_Network.IsHost && RemoteSession.IsConnectedToPlayer)
            {
                return MakeUniqueID(RemoteSession.ConnectedPlayer.m_SteamID, RemoteSession.ConnectedSessionID);
            }
            else if (!Raft_Network.IsHost && ClientSession.IsHostConnectedToPlayer)
            {
                return MakeUniqueID(ClientSession.ConnectedSteamID, ClientSession.ConnectedSessionID);
            }
            return null;
        }

        public static bool HasMetRaft(ulong steamID, string sessionID)
        {
            return Settings.MetRafts.ContainsKey(MakeUniqueID(steamID, sessionID));
        }
    }
}
