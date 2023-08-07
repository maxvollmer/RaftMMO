
using RaftMMO.ModSettings;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RaftMMO.Utilities
{
    public class PlayerCounter
    {
        public delegate void UpdateNumbersOfPlayersCallback(int numbersOfPlayers);
        public static UpdateNumbersOfPlayersCallback updateNumbersOfPlayersCallback;

        private static CSteamID previouslyJoinedLobby = CSteamID.Nil;
        private static List<CSteamID> allLobbies = new List<CSteamID>();
        private static int joinedLobbyIndex = -1;
        private static CallResult<LobbyMatchList_t> allLobbyListCallResult = null;
        private static CallResult<LobbyCreated_t> allLobbyCreatedCallResult = null;
        private static CallResult<LobbyEnter_t> allLobbyEnterCallResult = null;

        private static bool needsRefresh = true;

        private static object _numberOfPlayersLock = new object();

        public static int NumberOfPlayers { get; private set; } = 1;

        private static void UpdateNumberOfPlayers(int numberOfPlayers)
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.UpdateNumberOfPlayers{numberOfPlayers}");
                }
            }

            NumberOfPlayers = numberOfPlayers;
            updateNumbersOfPlayersCallback?.Invoke(numberOfPlayers);
        }



        public static void Update()
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.Update");
                }

                if (needsRefresh)
                {
                    needsRefresh = false;
                    Init();
                }
            }
        }

        private static void Init()
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.Init");
                }

                redoTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                redoTimer?.Dispose();
                redoTimer = null;

                allLobbyListCallResult?.Cancel();
                allLobbyCreatedCallResult?.Cancel();
                allLobbyEnterCallResult?.Cancel();
                allLobbyListCallResult = null;
                allLobbyCreatedCallResult = null;
                allLobbyEnterCallResult = null;

                if (joinedLobbyIndex >= 0 && joinedLobbyIndex < allLobbies.Count)
                {
                    previouslyJoinedLobby = allLobbies[joinedLobbyIndex];
                }

                allLobbies.Clear();
                joinedLobbyIndex = -1;

                SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
                SteamMatchmaking.AddRequestLobbyListStringFilter("id", Globals.LobbyAllName, ELobbyComparison.k_ELobbyComparisonEqual);
                allLobbyListCallResult = CallResult<LobbyMatchList_t>.Create(OnAllLobbyListResponse);
                allLobbyListCallResult.Set(SteamMatchmaking.RequestLobbyList());
            }
        }

        private static void OnAllLobbyListResponse(LobbyMatchList_t param, bool bIOFailure)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.OnAllLobbyListResponse");
                }

                if (param.m_nLobbiesMatching == 0)
                {
                    CreateNewLobby();
                }
                else
                {
                    int numberOfPlayers = 1;
                    for (uint i = 0; i < param.m_nLobbiesMatching; i++)
                    {
                        var allLobbySteamID = SteamMatchmaking.GetLobbyByIndex((int)i);
                        numberOfPlayers += SteamMatchmaking.GetNumLobbyMembers(allLobbySteamID);
                        allLobbies.Add(allLobbySteamID);
                    }
                    UpdateNumberOfPlayers(numberOfPlayers);
                    allLobbies.Shuffle();

                    joinedLobbyIndex = allLobbies.IndexOf(previouslyJoinedLobby);
                    if (joinedLobbyIndex >= 0)
                    {
                        JoinedLobby(joinedLobbyIndex);
                    }
                    else
                    {
                        TryJoinLobby(0);
                    }
                }
            }
        }

        private static void CreateNewLobby()
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.CreateNewLobby");
                }

                allLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnCreateAllLobby);
                allLobbyCreatedCallResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, 250));
            }
        }

        private static void OnCreateAllLobby(LobbyCreated_t param, bool bIOFailure)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.OnCreateAllLobby");
                }

                if (param.m_eResult == EResult.k_EResultOK)
                {
                    var steamid = new CSteamID(param.m_ulSteamIDLobby);
                    SteamMatchmaking.SetLobbyData(steamid, "id", Globals.LobbyAllName);
                    SteamMatchmaking.SetLobbyJoinable(steamid, true);
                    allLobbies.Add(steamid);
                    JoinedLobby(allLobbies.Count - 1);
                }
            }
        }

        private static void TryJoinLobby(int index)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.TryJoinLobby({index})");
                }

                joinedLobbyIndex = index;
                allLobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
                allLobbyEnterCallResult.Set(SteamMatchmaking.JoinLobby(allLobbies[index]));
            }
        }

        private static void OnLobbyEntered(LobbyEnter_t param, bool bIOFailure)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.OnLobbyEntered");
                }

                if (joinedLobbyIndex < 0 || joinedLobbyIndex >= allLobbies.Count || allLobbies[joinedLobbyIndex].m_SteamID != param.m_ulSteamIDLobby)
                {
                    RaftMMOLogger.LogWarning("OnLobbyEntered got invalid lobby: " + joinedLobbyIndex + ", " + allLobbies.Count);
                    return;
                }

                if (param.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
                {
                    JoinedLobby(joinedLobbyIndex);
                }
                else
                {
                    joinedLobbyIndex++;
                    if (joinedLobbyIndex < allLobbies.Count)
                    {
                        TryJoinLobby(joinedLobbyIndex);
                    }
                    else if (param.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseFull)
                    {
                        CreateNewLobby();
                    }
                }
            }
        }

        private static Timer redoTimer = null;

        private static void JoinedLobby(int index)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.JoinedLobby({index})");
                }

                joinedLobbyIndex = index;

                foreach(var lobbyID in allLobbies)
                {
                    if (SettingsManager.Settings.LogVerbose)
                    {
                        RaftMMOLogger.LogVerbose($"Lobby {lobbyID} player count: {SteamMatchmaking.GetNumLobbyMembers(lobbyID)}");
                    }
                }

                UpdateNumberOfPlayers(allLobbies.Sum(l => SteamMatchmaking.GetNumLobbyMembers(l)));

                redoTimer = new Timer(FlagForRedo, null, Globals.REDO_PLAYER_COUNTER_TIMEOUT, Timeout.Infinite);
            }
        }

        private static void FlagForRedo(object state)
        {
            lock (_numberOfPlayersLock)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose($"PlayerCounter.FlagForRedo");
                }

                redoTimer?.Dispose();
                redoTimer = null;
                needsRefresh = true;
            }
        }
    }
}
