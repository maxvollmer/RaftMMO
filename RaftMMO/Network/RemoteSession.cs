using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using HarmonyLib;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using RaftMMO.ModSettings;
using RaftMMO.Network.Messages;
using RaftMMO.Network.SerializableData.Simple;

namespace RaftMMO.Network
{
    public class RemoteSession
    {
        private class RequestingPlayer
        {
            public readonly Stopwatch timeSinceRequest;
            public readonly CSteamID steamID;
            public readonly string remoteSessionID;
            public readonly int handshake;

            public RequestingPlayer(CSteamID steamID, string remoteSessionID, int handshake)
            {
                timeSinceRequest = new Stopwatch();
                timeSinceRequest.Start();
                this.steamID = steamID;
                this.remoteSessionID = remoteSessionID;
                this.handshake = handshake;
            }
        }

        private static Stopwatch uploadLocalRaftStopwatch = new Stopwatch();
        private static Stopwatch uploadPosUpdateStopwatch = new Stopwatch();
        private static Stopwatch connectingToPlayerStopwatch = new Stopwatch();
        private static Stopwatch lastTimeWeReceivedRaftPosUpdate = new Stopwatch();
        private static Stopwatch joinLobbyStopwatch = new Stopwatch();
        private static Stopwatch switchLobbyCooldownStopWatch = new Stopwatch();

        private static List<RequestingPlayer> requestingPlayers = new List<RequestingPlayer>();

        private static List<CSteamID> lobbies = new List<CSteamID>();
        private static int joinedLobbyIndex = -1;

        private static CallResult<LobbyEnter_t> lobbyEnterCallResult = null;
        private static CallResult<LobbyMatchList_t> lobbyMatchListCallResult = null;
        private static CallResult<LobbyCreated_t> lobbyCreatedCallResult = null;

        private static List<CSteamID> lobbyPlayers = new List<CSteamID>();

        private static int connectedPlayerIndex = -1;
        private static bool isConnectingToPlayer = false;
        private static bool connectionRejected = false;

        private static bool isFirstPositionUpdate = true;

        private static bool IsInConnectionRange { get; set; } = false;

        public static string ConnectedSessionID { get; private set; } = null;

        public static int RemoteHandShake { get; private set; } = 0;
        public static int LocalHandShake { get; private set; } = 0;

        private static int raftUpdateCounter = 0;


        public static bool IsConnectedToPlayer
        {
            get
            {
                if (/*!IsInLobby() ||*/ isConnectingToPlayer || connectedPlayerIndex < 0 || connectedPlayerIndex >= lobbyPlayers.Count)
                    return false;

                if (!IsInConnectionRange || (lastTimeWeReceivedRaftPosUpdate.IsRunning && lastTimeWeReceivedRaftPosUpdate.ElapsedMilliseconds > 10000))
                {
                    MessageManager.SendDisconnectMessage(lobbyPlayers[connectedPlayerIndex], true, true);
                    connectedPlayerIndex = -1;
                    ConnectedSessionID = null;
                    RefreshPlayerList();
                    return false;
                }

                return true;
            }
        }

        public static bool IsConnectedPlayer(CSteamID steamID)
        {
            return IsConnectedToPlayer && SteamHelper.IsSameSteamID(steamID, ConnectedPlayer);
        }

        public static CSteamID ConnectedPlayer { get { return lobbyPlayers[connectedPlayerIndex]; } }

        public static void Update()
        {
            if (!Semih_Network.IsHost)
                return;

            if (BuoyManager.IsCloseEnoughToConnect())
            {
                IsInConnectionRange = true;
            }
            if (BuoyManager.IsFarEnoughToDisconnect())
            {
                if (IsInConnectionRange)
                {
                    Disconnect();
                }
                IsInConnectionRange = false;
            }

            if (!IsInConnectionRange)
            {
                if (IsConnectedToPlayer)
                {
                    Disconnect();
                }
                CancelLobbyJoining();
                return;
            }

            if (!IsConnectedToPlayer)
            {
                if (RemoteHandShake != 0)
                {
                    RemoteHandShake = 0;
                    LocalHandShake = 0;
                }
                while (LocalHandShake == 0)
                {
                    LocalHandShake = Guid.NewGuid().GetHashCode();
                }

                RemoteRaft.SetLastUpdateCounter(-1);
                raftUpdateCounter = 0;
                isFirstPositionUpdate = true;
                Globals.RemotePosRotation = 360f;
                uploadPosUpdateStopwatch.Stop();
                uploadLocalRaftStopwatch.Stop();
                ConnectToRandomPlayerWithRaftMMOMod();
            }

            if (IsConnectedToPlayer || IsInLobby())
            {
                MessageManager.ReceiveRaftMMOMessages();
            }

            if (IsConnectedToPlayer)
            {
                // leave lobby, so players don't try to connect to us while we are already connected
                if (IsInLobby())
                {
                    SteamMatchmaking.LeaveLobby(lobbies[joinedLobbyIndex]);
                    joinedLobbyIndex = -1;
                    lobbies.Clear();
                }

                if (!uploadPosUpdateStopwatch.IsRunning || uploadPosUpdateStopwatch.ElapsedMilliseconds >= Globals.PositionUpdateFrequency)
                {
                    MessageManager.UploadLocalPosUpdates(lobbyPlayers[connectedPlayerIndex]);
                    uploadPosUpdateStopwatch.Restart();
                }

                if (!uploadLocalRaftStopwatch.IsRunning || uploadLocalRaftStopwatch.ElapsedMilliseconds >= Globals.RaftUpdateFrequency)
                {
                    raftUpdateCounter = MessageManager.UploadLocalRaft(lobbyPlayers[connectedPlayerIndex], raftUpdateCounter);
                    MessageManager.UploadListOfPlayers(lobbyPlayers[connectedPlayerIndex], ClientSession.GetSessionPlayers().Select(p => p.steamID.m_SteamID).ToArray());
                    uploadLocalRaftStopwatch.Restart();
                }
            }
        }

        public static void Disconnect()
        {
            if (IsConnectedToPlayer)
            {
                MessageManager.SendDisconnectMessage(lobbyPlayers[connectedPlayerIndex], true, true);
            }

            CancelLobbyJoining();

            lobbies.Clear();
            lobbyPlayers.Clear();
            requestingPlayers.Clear();

            uploadLocalRaftStopwatch.Stop();
            uploadPosUpdateStopwatch.Stop();
            connectingToPlayerStopwatch.Stop();
            lastTimeWeReceivedRaftPosUpdate.Stop();

            connectedPlayerIndex = -1;
            ConnectedSessionID = null;
            isConnectingToPlayer = false;
            connectionRejected = false;

            isFirstPositionUpdate = true;
        }

        private static void ConnectToRandomPlayerWithRaftMMOMod()
        {
            if (IsConnectedToPlayer)
                return;

            // Wait for connection to a lobby. If it times out, we try again.
            if (joinLobbyStopwatch.IsRunning)
            {
                if (joinLobbyStopwatch.ElapsedMilliseconds < Globals.ConnectToLobbyTimeout)
                {
                    return;
                }
                CancelLobbyJoining();
            }

            if (!IsInLobby())
            {
                RequestLobbyListAndJoinOne();
                return;
            }

            if (isConnectingToPlayer && connectingToPlayerStopwatch.IsRunning && connectingToPlayerStopwatch.ElapsedMilliseconds < Globals.ConnectToPlayerTimeout && !connectionRejected)
            {
                return;
            }

            connectionRejected = false;

            if (requestingPlayers.Count > 0)
            {
                bool hasAcceptedAny = false;
                foreach (var requestingPlayer in requestingPlayers.OrderBy(rP => rP.timeSinceRequest.ElapsedMilliseconds))
                {
                    bool hasAccepted = false;

                    if (!hasAcceptedAny
                        && requestingPlayer.timeSinceRequest.ElapsedMilliseconds < 5000
                        && !SettingsManager.IsBlockedRaft(requestingPlayer.steamID.m_SteamID, requestingPlayer.remoteSessionID)
                        && (BuoyManager.IsCloseEnoughToConnectToAll() || SettingsManager.IsFavoritedRaft(requestingPlayer.steamID.m_SteamID, requestingPlayer.remoteSessionID)))
                    {
                        for (int i = 0; i < lobbyPlayers.Count; i++)
                        {
                            if (lobbyPlayers[i] == requestingPlayer.steamID)
                            {
                                hasAccepted = true;
                                hasAcceptedAny = true;
                                connectedPlayerIndex = i;

                                AcceptConnectionRequest(requestingPlayer.steamID, requestingPlayer.remoteSessionID, requestingPlayer.handshake);
                            }
                        }
                    }

                    if (!hasAccepted)
                    {
                        RejectConnectionRequest(requestingPlayer.steamID);
                    }
                }
                requestingPlayers.Clear();
                return;
            }

            if (switchLobbyCooldownStopWatch.IsRunning)
            {
                if (switchLobbyCooldownStopWatch.ElapsedMilliseconds >= Globals.SwitchLobbyCooldownTimeout)
                {
                    switchLobbyCooldownStopWatch.Stop();
                    SwitchLobby();
                }
                return;
            }

            if (!Globals.TEMPDEBUGStaticBuoyPosition && BuoyManager.IsTooCloseToActivelyConnect())
                return;

            connectedPlayerIndex++;
            if (connectedPlayerIndex < lobbyPlayers.Count)
            {
                SendConnectionRequest(lobbyPlayers[connectedPlayerIndex]);
                isConnectingToPlayer = true;
                connectingToPlayerStopwatch.Restart();
                lastTimeWeReceivedRaftPosUpdate.Stop();
            }
            else
            {
                connectingToPlayerStopwatch.Stop();
                lastTimeWeReceivedRaftPosUpdate.Stop();
                connectedPlayerIndex = -1;
                isConnectingToPlayer = false;
                switchLobbyCooldownStopWatch.Restart();
            }
        }

        private static void AcceptConnectionRequest(CSteamID steamID, string remoteSessionID, int handshake)
        {
            RaftMMOLogger.LogVerbose("AcceptConnectionRequest: " + handshake);

            if (SettingsManager.IsBlockedRaft(steamID.m_SteamID, remoteSessionID)
                || (!BuoyManager.IsCloseEnoughToConnectToAll() && !SettingsManager.IsFavoritedRaft(steamID.m_SteamID, remoteSessionID)))
            {
                RejectConnectionRequest(steamID);
                return;
            }

            MessageManager.SendMessage(steamID, new AcceptConnectionMessage(handshake));
            DoConnect(remoteSessionID, handshake);

            RaftMMOLogger.LogVerbose("AcceptConnectionRequest Done!");
        }

        private static void RejectConnectionRequest(CSteamID steamID)
        {
            RaftMMOLogger.LogVerbose("RejectConnectionRequest");

            MessageManager.SendMessage(steamID, new BaseMessage(MessageType.REJECT_CONNECTION, true));

            RaftMMOLogger.LogVerbose("RejectConnectionRequest Done!");
        }

        private static void SendConnectionRequest(CSteamID steamID)
        {
            RaftMMOLogger.LogVerbose("SendConnectionRequest");

            MessageManager.SendMessage(steamID, new RequestConnectionMessage());

           RaftMMOLogger.LogVerbose("SendConnectionRequest Done!");
        }

        public static void HandleRequestConnection(CSteamID steamID, string remoteSessionID, int handshake)
        {
           RaftMMOLogger.LogVerbose("HandleRequestConnection: " + IsConnectedToPlayer + ", handshake: " + handshake);

            if (SettingsManager.IsBlockedRaft(steamID.m_SteamID, remoteSessionID)
                || (!BuoyManager.IsCloseEnoughToConnectToAll() && !SettingsManager.IsFavoritedRaft(steamID.m_SteamID, remoteSessionID)))
            {
                RejectConnectionRequest(steamID);
                return;
            }

            if (IsConnectedToPlayer)
            {
                // should never happen, but just to be sure
                if (SteamHelper.IsSameSteamID(steamID, lobbyPlayers[connectedPlayerIndex]))
                {
                    AcceptConnectionRequest(steamID, remoteSessionID, handshake);
                }
                else
                {
                    RejectConnectionRequest(steamID);
                }
            }
            else if (isConnectingToPlayer
                && connectedPlayerIndex >= 0 && connectedPlayerIndex < lobbyPlayers.Count
                && lobbyPlayers[connectedPlayerIndex] == steamID)
            {
                AcceptConnectionRequest(steamID, remoteSessionID, handshake);
            }
            else
            {
                requestingPlayers.Add(new RequestingPlayer(steamID, remoteSessionID, handshake));
            }

           RaftMMOLogger.LogVerbose("HandleRequestConnection Done: " + requestingPlayers.Count);
        }

        public static void HandleAcceptConnection(CSteamID steamID, string remoteSessionID, int localhandshake, int remotehandshake)
        {
            RaftMMOLogger.LogVerbose("HandleAcceptConnection: " + isConnectingToPlayer + ", localhandshake: " + localhandshake + ", remotehandshake: " + remotehandshake);

            if (isConnectingToPlayer
                && connectedPlayerIndex >= 0 && connectedPlayerIndex < lobbyPlayers.Count
                && SteamHelper.IsSameSteamID(steamID, lobbyPlayers[connectedPlayerIndex])
                && !SettingsManager.IsBlockedRaft(steamID.m_SteamID, remoteSessionID)
                && (BuoyManager.IsCloseEnoughToConnectToAll() || SettingsManager.IsFavoritedRaft(steamID.m_SteamID, remoteSessionID))
                && localhandshake == LocalHandShake)
            {
                DoConnect(remoteSessionID, remotehandshake);
            }
            else if (IsConnectedPlayer(steamID) && localhandshake == LocalHandShake)
            {
                DoConnect(remoteSessionID, remotehandshake);
            }
            else
            {
                // Send disconnect to other side, so they don't wait for a timeout while thinking we are connected
                MessageManager.SendDisconnectMessage(steamID, true, false, remotehandshake);
            }

            RaftMMOLogger.LogVerbose("HandleAcceptConnection Done: " + isConnectingToPlayer);
        }

        private static void DoConnect(string remoteSessionID, int handshake)
        {
            RaftMMOLogger.LogVerbose("DoConnect: " + remoteSessionID + ", handshake: " + handshake);

            ConnectedSessionID = remoteSessionID;
            RemoteHandShake = handshake;

            isConnectingToPlayer = false;
            connectingToPlayerStopwatch.Stop();

            isFirstPositionUpdate = true;

            Globals.FullRaftUpdateRequested = true;
            Globals.RemotePosRotation = 360f;

            lastTimeWeReceivedRaftPosUpdate.Restart();

            RaftMMOLogger.LogVerbose("DoConnect Done!");
        }

        public static void HandleRejectConnection(CSteamID steamID)
        {
           RaftMMOLogger.LogVerbose("HandleRejectConnection: " + connectionRejected);

            if (isConnectingToPlayer
                && connectedPlayerIndex >= 0 && connectedPlayerIndex < lobbyPlayers.Count
                && SteamHelper.IsSameSteamID(steamID, lobbyPlayers[connectedPlayerIndex]))
            {
                connectionRejected = true;
            }

           RaftMMOLogger.LogVerbose("HandleRejectConnection Done: " + connectionRejected);
        }

        public static void HandleRaftPositionUpdate(IPositionUpdateMessage message)
        {
            lastTimeWeReceivedRaftPosUpdate.Restart();

            RaftMMOLogger.LogVerbose("HandleRaftPositionUpdate");

            var raft = ComponentManager<Raft>.Value;

            // TODO: Sanitize incoming values. E.g. reject teleportation.
            var remotePosition = message.Position.Vector3;

            if (isFirstPositionUpdate)
            {
                // Need to account for relative offset/rotation to local raft around meeting point
                if (remotePosition.magnitude > 0)
                {
                    Vector3 localDelta = (raft.transform.position - Globals.CurrentRaftMeetingPoint).normalized;
                    Vector3 remoteDelta = remotePosition.normalized;

                    float angle = Vector3.SignedAngle(localDelta, remoteDelta, Vector3.up);
                    if (angle < 0f)
                    {
                        Globals.RemotePosRotation = -180f - angle;
                    }
                    else
                    {
                        Globals.RemotePosRotation = 180f - angle;
                    }
                }
                else
                {
                    Globals.RemotePosRotation = 180f;
                }
                isFirstPositionUpdate = false;
            }

            if (Globals.RemotePosRotation != 360f && message.RemotePosRotation != 360f)
            {
                // Sync/sanitize rotations
                // Bigger number wins, so if this ever gets ouf of sync, the sessions will agree on the max and be in sync again
                if (Math.Abs(message.RemotePosRotation) > Math.Abs(Globals.RemotePosRotation))
                {
                    Globals.RemotePosRotation = -message.RemotePosRotation;
                }
            }

            Vector3 targetPos = Globals.CurrentRaftMeetingPoint + (Quaternion.AngleAxis(Globals.RemotePosRotation, Vector3.up) * remotePosition);
            Quaternion targetRot = Quaternion.Euler(message.Rotation.X, message.Rotation.Y + Globals.RemotePosRotation, message.Rotation.Z);

            // Push remote raft away if too far from buoy
            float distanceToBuoy = raft.transform.position.DistanceXZ(Globals.CurrentRaftMeetingPoint);
            float remoteDistanceToBuoy = targetPos.DistanceXZ(Globals.CurrentRaftMeetingPoint);
            float pushDistance = Math.Max(distanceToBuoy - 250f, remoteDistanceToBuoy - 250f);
            if (pushDistance > 0f)
            {
                Vector3 direction = targetPos - raft.transform.position;
                direction.y = 0f;
                if (direction.magnitude == 0f)
                {
                    direction = new Vector3(0f, 0f, 1f);
                }
                Globals.CurrentPushAwayOffset = direction.normalized * pushDistance;
            }
            else
            {
                Globals.CurrentPushAwayOffset = Vector3.zero;
            }

            RemoteRaft.MoveTo(targetPos + Globals.CurrentPushAwayOffset, targetRot);

            RaftMMOLogger.LogVerbose("HandleRaftPositionUpdate Done!");
        }

        public static void HandleDisconnect(CSteamID steamID)
        {
            if (IsConnectedPlayer(steamID))
            {
                MessageManager.SendDisconnectMessage(steamID, false, true);
                connectedPlayerIndex = -1;
                ConnectedSessionID = null;
                RefreshPlayerList();
            }
        }

        public static void HandlePlayerPositionUpdate(Messages.PlayerUpdateMessage message)
        {
            if (SettingsManager.IsBlockedPlayer(message.steamID))
            {
                RemoteRaft.RemoveRemotePlayer(message.steamID);
                return;
            }

            var translatedPos = Globals.CurrentRaftMeetingPoint + Globals.CurrentPushAwayOffset
                + Quaternion.AngleAxis(Globals.RemotePosRotation, Vector3.up) * message.position.Vector3;

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
            Traverse.Create(player.PersonController).Field("networkPositionIsLocalLockedPivot").SetValue(message.raftAttachStatus == RaftAttachStatus.ATTACHED_TO_REMOTE_RAFT);

            relativePositionFaker.transform.SetParent(null);
            Object.Destroy(relativePositionFaker);

            // TEMPDebugMayasHair(player);
        }

        private static void TEMPDebugMayasHair(Network_Player player)
        {
            var hair = player.gameObject.transform.FindChildRecursively("Female_Hair_Full");
            if (hair == null)
                return;

            RaftMMOLogger.LogDebug("hair.parent.parent: " + hair.parent.parent + ", " + hair.parent.parent.localPosition + ", " + hair.parent.parent.localRotation.eulerAngles);
            RaftMMOLogger.LogDebug("hair: " + hair + ", " + hair.position + ", " + hair.rotation.eulerAngles);

            // GameObjectDebugger.DebugPrint(hair.gameObject);

            // BuoyManager.DebugForceSetBuoy(hair.position);

            var renderer = hair.GetComponent<SkinnedMeshRenderer>();

            var mesh =  renderer.sharedMesh;
            var material = renderer.material;
            var sharedMaterial = renderer.sharedMaterial;

            GameObject test = new GameObject();
            var newRenderer = test.AddComponent<SkinnedMeshRenderer>();
            newRenderer.sharedMesh = mesh;
            newRenderer.material = material;
            newRenderer.sharedMaterial = sharedMaterial;
            test.transform.position = new Vector3(0f, 3f, 0f);
           // test.transform.SetParent(BuoyManager.buoy.transform, false);

           // hair.parent = BuoyManager.buoy.transform;
            hair.gameObject.SetActive(false);

            var debugmsg = "rootBone: " + renderer.rootBone.localPosition
                + ", " + renderer.rootBone.localRotation.eulerAngles;
            foreach (var bone in renderer.bones)
            {
                debugmsg += "\nbone: " + bone.localPosition + ", " + bone.localRotation.eulerAngles;
            }
           RaftMMOLogger.LogDebug(debugmsg);
            //var bone = renderer.bones
            //renderer.
        }

        private static void RefreshPlayerList()
        {
           RaftMMOLogger.LogVerbose("RefreshPlayerList");

            connectedPlayerIndex = -1;
            ConnectedSessionID = null;
            isConnectingToPlayer = false;
            lobbyPlayers.Clear();

            if (!IsInLobby())
                return;

            SteamMatchmaking.JoinLobby(lobbies[joinedLobbyIndex]);
            string data = SteamMatchmaking.GetLobbyData(lobbies[joinedLobbyIndex], "id");

            int numplayers = SteamMatchmaking.GetNumLobbyMembers(lobbies[joinedLobbyIndex]);

            Network_Player localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            for (int i = 0; i < numplayers; i++)
            {
                CSteamID playerSteamID = SteamMatchmaking.GetLobbyMemberByIndex(lobbies[joinedLobbyIndex], i);

                if (SettingsManager.IsBlockedPlayer(playerSteamID.m_SteamID))
                    continue;

                if (!BuoyManager.IsCloseEnoughToConnectToAll() && !SettingsManager.GetFavoritedRaftsByDate.Any(raftEntry => SteamHelper.IsSameSteamID(raftEntry.steamID, playerSteamID.m_SteamID)))
                    continue;

                // don't connect to yourself unless we are debugging locally
                if (playerSteamID == localPlayer.steamID && !Globals.TEMPDEBUGConnectToLocalPlayer)
                    continue;

                // if the setting is enabled, only connect to steam friends
                if (SettingsManager.Settings.OnlyMeetSteamFriends
                    && SteamFriends.GetFriendRelationship(playerSteamID) != EFriendRelationship.k_EFriendRelationshipFriend
                    && playerSteamID != localPlayer.steamID)
                    continue;

                SteamHelper.Connect(playerSteamID);
                lobbyPlayers.Add(playerSteamID);
            }
            lobbyPlayers.Shuffle();

           RaftMMOLogger.LogVerbose("RefreshPlayerList Done: ", lobbyPlayers.Count);
        }

        private static void CancelLobbyJoining()
        {
           RaftMMOLogger.LogVerbose("CancelLobbyJoining");

            if (IsInLobby())
            {
                SteamMatchmaking.LeaveLobby(lobbies[joinedLobbyIndex]);
            }

            joinedLobbyIndex = -1;
            lobbies.Clear();
            joinLobbyStopwatch.Stop();
            switchLobbyCooldownStopWatch.Stop();
            lobbyMatchListCallResult?.Cancel();
            lobbyEnterCallResult?.Cancel();
            lobbyCreatedCallResult?.Cancel();
            lobbyMatchListCallResult = null;
            lobbyEnterCallResult = null;
            lobbyCreatedCallResult = null;
            connectedPlayerIndex = -1;
            ConnectedSessionID = null;
            isConnectingToPlayer = false;
            lobbyPlayers.Clear();

           RaftMMOLogger.LogVerbose("CancelLobbyJoining Done!");
        }

        private static void SwitchLobby()
        {
           RaftMMOLogger.LogVerbose("SwitchLobby");

            if (IsInLobby())
            {
                if (lobbyPlayers.Count < 250 && lobbies.Count == 1)
                {
                    RefreshPlayerList();
                    return;
                }
                SteamMatchmaking.LeaveLobby(lobbies[joinedLobbyIndex]);
            }
            connectedPlayerIndex = -1;
            ConnectedSessionID = null;
            isConnectingToPlayer = false;
            lobbyPlayers.Clear();
            TryJoinLobby(joinedLobbyIndex + 1);

           RaftMMOLogger.LogVerbose("SwitchLobby Done!");
        }

        private static void RequestLobbyListAndJoinOne()
        {
            CancelLobbyJoining();

            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter("id", "de.maxvollmer.raftmmo.lobby", ELobbyComparison.k_ELobbyComparisonEqual);
            lobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnRequestLobbyListResponse);
            lobbyMatchListCallResult.Set(SteamMatchmaking.RequestLobbyList());

            joinLobbyStopwatch.Restart();
        }

        private static void OnRequestLobbyListResponse(LobbyMatchList_t param, bool bIOFailure)
        {
           RaftMMOLogger.LogVerbose("OnRequestLobbyListResponse: ", param.m_nLobbiesMatching);

            if (param.m_nLobbiesMatching == 0)
            {
                CreateNewLobby();
            }
            else
            {
                for (uint i = 0; i < param.m_nLobbiesMatching; i++)
                {
                    lobbies.Add(SteamMatchmaking.GetLobbyByIndex((int)i));
                }
                lobbies.Shuffle();
                TryJoinLobby(0);
            }

           RaftMMOLogger.LogVerbose("OnRequestLobbyListResponse Done!");
        }

        private static void OnLobbyEntered(LobbyEnter_t param, bool bIOFailure)
        {
            // ignore invalid lobbies
            if (joinedLobbyIndex < 0 || joinedLobbyIndex >= lobbies.Count
                || lobbies[joinedLobbyIndex].m_SteamID != param.m_ulSteamIDLobby)
            {
                RaftMMOLogger.LogWarning("OnLobbyEntered got invalid lobby: " + joinedLobbyIndex + ", " + lobbies.Count);
                return;
            }

           RaftMMOLogger.LogVerbose("OnLobbyEntered");

            if (param.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                JoinedLobby(joinedLobbyIndex);
            }
            else
            {
                joinedLobbyIndex++;
                if (joinedLobbyIndex < lobbies.Count)
                {
                    TryJoinLobby(joinedLobbyIndex);
                }
                else if (param.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseFull)
                {
                    CreateNewLobby();
                }
                else
                {
                    CancelLobbyJoining();
                }
            }

           RaftMMOLogger.LogVerbose("OnLobbyEntered Done!");
        }

        private static void TryJoinLobby(int index)
        {
            if (index >= 0 && index < lobbies.Count)
            {
                joinedLobbyIndex = index;
                lobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
                lobbyEnterCallResult.Set(SteamMatchmaking.JoinLobby(lobbies[index]));
                joinLobbyStopwatch.Restart();
            }
            else
            {
                CancelLobbyJoining();
            }
        }

        private static void CreateNewLobby()
        {
           RaftMMOLogger.LogVerbose("CreateNewLobby");

            lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnCreateLobby);
            lobbyCreatedCallResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, 250));
            joinLobbyStopwatch.Restart();

           RaftMMOLogger.LogVerbose("CreateNewLobby Done!");
        }

        private static void OnCreateLobby(LobbyCreated_t param, bool bIOFailure)
        {
           RaftMMOLogger.LogVerbose("OnCreateLobby");

            if (param.m_eResult == EResult.k_EResultOK)
            {
                var steamid = new CSteamID(param.m_ulSteamIDLobby);
                SteamMatchmaking.SetLobbyData(steamid, "id", "de.maxvollmer.raftmmo.lobby");
                SteamMatchmaking.SetLobbyJoinable(steamid, true);
                lobbies.Add(steamid);
                JoinedLobby(lobbies.Count - 1);
            }
            else
            {
                CancelLobbyJoining();
            }

            RaftMMOLogger.LogVerbose("OnCreateLobby Done: ", param.m_eResult, " (", joinedLobbyIndex, ", ", lobbies.Count, ")");
        }

        private static void JoinedLobby(int index)
        {
            if (index < 0 || index >= lobbies.Count)
            {
                CancelLobbyJoining();
            }
            else
            {
                joinedLobbyIndex = index;
                joinLobbyStopwatch.Stop();
            }
        }

        private static bool IsInLobby()
        {
            return !joinLobbyStopwatch.IsRunning && joinedLobbyIndex >= 0 && joinedLobbyIndex < lobbies.Count;
        }
    }
}
