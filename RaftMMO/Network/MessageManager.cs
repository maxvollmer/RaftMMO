using RaftMMO.ModSettings;
using RaftMMO.Network.Messages;
using RaftMMO.RaftCopyTools;
using RaftMMO.Trade;
using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace RaftMMO.Network
{
    public class MessageManager
    {
        private static void SerializeMessage(BaseMessage message, Stream stream)
        {
            using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress, true))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(gzip, message);
            }
        }

        private static BaseMessage DeserializeMessage(Stream stream)
        {
            try
            {
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress, true))
                {
                    BinaryFormatter formatter = new BinaryFormatter
                    {
                        Binder = new RaftMMODeserializationBinder()
                    };
                    object message = formatter.Deserialize(gzip);
                    if (message is BaseMessage raftMMOMessage)
                    {
                        return raftMMOMessage;
                    }
                    else if (message == null)
                    {
                        RaftMMOLogger.LogWarning("DeserializeMessage got null message");
                        return null;
                    }
                    else
                    {
                        RaftMMOLogger.LogWarning("DeserializeMessage got invalid message type: " + message.GetType());
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                RaftMMOLogger.LogError("DeserializeMessage caught exception: " + e);
                return null;
            }
        }


        /*
        private static readonly string MESSAGE_DELIMITER = "LOL";

        private static void SerializeMessage(BaseMessage message, Stream stream)
        {
            using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress, true))
            {
                string typename = message.GetType().FullName;
                string json = JsonConvert.SerializeObject(message, Formatting.None, new JsonSerializerSettings { ContractResolver = new RaftMMOJsonContractResolver() });
                byte[] bytes = Encoding.UTF8.GetBytes(typename + MESSAGE_DELIMITER + json);
                gzip.Write(bytes, 0, bytes.Length);
            }
        }

        private static BaseMessage DeserializeMessage(Stream stream)
        {
            try
            {
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress, true))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        gzip.CopyTo(ms);
                        string message = Encoding.UTF8.GetString(ms.ToArray());
                        int delimindex = message.IndexOf(MESSAGE_DELIMITER);
                        if (delimindex > 0)
                        {
                            string typename = message.Substring(0, delimindex);
                            string json = message.Substring(delimindex + MESSAGE_DELIMITER.Length);
                            return JsonConvert.DeserializeObject(json, RaftMMODeserializationBinder.GetType(typename)) as BaseMessage;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RaftMMOLogger.LogError("DeserializeMessage caught exception: " + e);
            }
            return null;
        }
        */


        public static void SendConnectedMessage(CSteamID steamID)
        {
            bool sendresult1 = SendMessage(steamID, new ConnectedMessage());
        }

        public static void UploadBuoys(CSteamID steamID)
        {
            // TODO: Send buoy positions and current raft meeting point
            // BuoyManager.BuoyLocations
            // Globals.CurrentRaftMeetingPoint
            // Globals.CurrentRaftMeetingPointDistance

            bool sendresult1 = SendMessage(steamID, new BuoysUpdateMessage());
        }

        public static bool SendMessage(CSteamID steamID, BaseMessage message)
        {
            bool acceptresult = SteamHelper.Connect(steamID);
            RaftMMOLogger.LogVerbose("SendMessage: " + message.type + ", " + acceptresult);

            MemoryStream stream = new MemoryStream();
            SerializeMessage(message, stream);
            byte[] array = stream.ToArray();

            if (SettingsManager.Settings.LogVerbose)
            {
                MemoryStream stream2 = new MemoryStream();
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream2, message);
                byte[] array2 = stream2.ToArray();
                RaftMMOLogger.LogVerbose("Compression for " + message.type + ": "
                    + array2.Length + " => " + array.Length
                    + " (" + ((int)(array.Length * 100.0 / array2.Length)) + "%)");
            }

            bool result = true;

            if (message.reliable && array.Length > Globals.ReliableMessageSizeLimit)
            {
                int splitsize = (Globals.ReliableMessageSizeLimit * 80 / 100);

                int nummessages = array.Length / splitsize;
                if (array.Length % splitsize != 0)
                {
                    nummessages += 1;
                }
                int id = Guid.NewGuid().GetHashCode();
                RaftMMOLogger.LogVerbose("SendMessage: message exceeds limit " + Globals.ReliableMessageSizeLimit + " (" + array.Length + "), splitting into " + nummessages + " split messages of size " + splitsize + " with id: " + id);
                for (int i = 0; i < nummessages; i++)
                {
                    result = result && SendMessage(steamID, new SplitMessage(id, i, nummessages, array.SubArray(i * splitsize, Math.Min(array.Length - i * splitsize, splitsize))));
                }
            }
            else
            {
                result = SteamNetworking.SendP2PPacket(
                steamID,
                array, (uint)array.Length,
                message.reliable ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliableNoDelay,
                1337);
            }

            Network_Player localPlayer = ComponentManager<Raft_Network>.Value.GetLocalPlayer();

            if (!result)
            {
                RaftMMOLogger.LogError("SendMessage failed: " + message.type
                    + ", size: " + array.Length
                    + ", steamID: " + steamID
                    + ", local steamID: " + localPlayer.steamID);
            }
            else
            {
                RaftMMOLogger.LogVerbose("SendMessage Done!");
            }

            return result;
        }

        public static void UploadLocalPosUpdates(CSteamID connectedPlayer)
        {
            RaftMMOLogger.LogVerbose("UploadPosUpdates");

            var raft = ComponentManager<Raft>.Value;

            bool sendresult1 = SendMessage(connectedPlayer,
                new PositionUpdateMessage(
                    raft.transform.position - Globals.CurrentRaftMeetingPoint,
                    raft.transform.rotation,
                    Globals.RemotePosRotation));

            foreach (var player in ComponentManager<Raft_Network>.Value.remoteUsers.Values)
            {
                if (player != null)
                {
                    RaftAttachStatus raftAttachStatus = RaftAttachStatus.NOT_ATTACHED;
                    if (player.PersonController.transform.parent == SingletonGeneric<GameManager>.Singleton.lockedPivot)
                    {
                        raftAttachStatus = RaftAttachStatus.ATTACHED_TO_OWN_RAFT;
                    }
                    else if (player.PersonController.transform.parent == RemoteRaft.Transform)
                    {
                        raftAttachStatus = RaftAttachStatus.ATTACHED_TO_REMOTE_RAFT;
                    }

                    bool sendresult2 = SendMessage(connectedPlayer, new PlayerUpdateMessage(player, raftAttachStatus));
                    RaftMMOLogger.LogVerbose("sendresult2: " + sendresult2);
                }
            }

            RaftMMOLogger.LogVerbose("UploadPosUpdates Done!");
        }

        public static void SendDisconnectMessage(CSteamID connectedPlayer, bool sendtohost, bool sendtoclients, int? overridehandshake = null)
        {
            if (sendtohost)
            {
                SendMessage(connectedPlayer, new DisconnectMessage(overridehandshake));
            }

            if (sendtoclients)
            {
                foreach (var client in ClientSession.GetSessionPlayers())
                {
                    SendMessage(client.steamID, new DisconnectMessage(overridehandshake));
                }
            }

            SteamHelper.Close(connectedPlayer);
        }

        public static void SendTradeUpdate(List<SerializableData.Item> offerItems, List<SerializableData.Item> wishItems, ulong remoteTradePlayerSteamID, bool isAcceptingTrade)
        {
            if (Raft_Network.IsHost)
            {
                if (RemoteSession.IsConnectedToPlayer)
                {
                    SendMessage(RemoteSession.ConnectedPlayer, new TradeMessage(offerItems, wishItems, remoteTradePlayerSteamID, isAcceptingTrade));
                }
            }
            else if (ComponentManager<Raft_Network>.Value.IsConnectedToHost)
            {
                SendMessage(ComponentManager<Raft_Network>.Value.HostID, new TradeMessage(offerItems, wishItems, remoteTradePlayerSteamID, isAcceptingTrade));
            }
        }

        public static void SendCompleteTradeMessage(CompleteTradeMessage message)
        {
            if (Raft_Network.IsHost && RemoteSession.IsConnectedToPlayer)
            {
                SendMessage(RemoteSession.ConnectedPlayer, message);
            }
            else if (!Raft_Network.IsHost && ComponentManager<Raft_Network>.Value.IsConnectedToHost && ClientSession.IsHostConnectedToPlayer)
            {
                SendMessage(ComponentManager<Raft_Network>.Value.HostID, message);
            }
        }

        public static int UploadLocalRaft(CSteamID connectedPlayer, int counter)
        {
            RaftDeltaMessage message = RaftDataManager.Remote.CreateRaftDeltaMessage(RaftCopier.CreateRaftData(), Globals.FullRaftUpdateRequested, false);

            if (!Globals.FullRaftUpdateRequested
                && message.added_data.blockData.Length == 0
                && message.removed_data.blockData.Length == 0)
                return counter;

            if (Globals.FullRaftUpdateRequested)
            {
                counter = 0;
            }

            message.counter = counter;

            RaftMMOLogger.LogVerbose("UploadLocalRaft: " + Globals.FullRaftUpdateRequested + ", " + message.counter);

            Globals.FullRaftUpdateRequested = false;

            bool sendresult = SendMessage(connectedPlayer, message);

            RaftMMOLogger.LogVerbose("UploadLocalRaft Done: " + sendresult);

            return counter + 1;
        }

        public static void UploadRemotePosUpdates(CSteamID connectedPlayer)
        {
            RaftMMOLogger.LogVerbose("UploadPosUpdates");

            bool sendresult1 = SendMessage(connectedPlayer,
                new PositionUpdateMessage(
                    RemoteRaft.Transform.position,
                    RemoteRaft.Transform.rotation,
                    0f));

            foreach (var player in RemoteRaft.GetRemotePlayers())
            {
                if (player != null)
                {
                    RaftAttachStatus raftAttachStatus = RaftAttachStatus.NOT_ATTACHED;
                    if (player.PersonController.transform.parent == SingletonGeneric<GameManager>.Singleton.lockedPivot)
                    {
                        raftAttachStatus = RaftAttachStatus.ATTACHED_TO_REMOTE_RAFT;
                    }
                    else if (player.PersonController.transform.parent == RemoteRaft.Transform)
                    {
                        raftAttachStatus = RaftAttachStatus.ATTACHED_TO_OWN_RAFT;
                    }

                    bool sendresult2 = SendMessage(connectedPlayer, new PlayerUpdateMessage(player, raftAttachStatus));
                    RaftMMOLogger.LogVerbose("sendresult2: " + sendresult2);
                }
            }

            RaftMMOLogger.LogVerbose("UploadPosUpdates Done: " + sendresult1);
        }

        public static int UploadRemoteRaft(CSteamID player, bool full, int counter)
        {
            RaftDeltaMessage message = RaftDataManager.Client(player.m_SteamID).CreateRaftDeltaMessage(RemoteRaft.GetRaftData(), full, true);

            if (!full
                && message.added_data.blockData.Length == 0
                && message.removed_data.blockData.Length == 0)
                return counter;

            if (full)
            {
                counter = 0;
            }

            message.counter = counter;

            RaftMMOLogger.LogVerbose("UploadRemoteRaft to " + player.m_SteamID + ", " + full + ", message.counter: " + message.counter);

            bool sendresult = SendMessage(player, message);

            RaftMMOLogger.LogVerbose("UploadRemoteRaft Done: " + sendresult);

            return counter + 1;
        }

        public static void UploadListOfPlayers(CSteamID steamID, ulong[] players)
        {
            bool sendresult = SendMessage(steamID, new PlayerListMessage(players));
        }

        public static void ReceiveRaftMMOMessages()
        {
            RaftMMOLogger.LogVerbose("ReceiveRaftMMOMessages");

            uint countofmessages = 0;
            uint messagereadsuccesscount = 0;
            while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize, 1337))
            {
                countofmessages++;
                byte[] array = new byte[msgSize];
                if (SteamNetworking.ReadP2PPacket(array, msgSize, out _, out CSteamID remoteSteamID, 1337))
                {
                    var message = DeserializeMessage(new MemoryStream(array));

                    if (TryHandleMessage(remoteSteamID, message))
                    {
                        messagereadsuccesscount++;
                    }
                }
            }

            RaftMMOLogger.LogVerbose("ReceiveRaftMMOMessages Done (countofmessages: " + countofmessages + ", messagereadsuccesscount: " + messagereadsuccesscount + ")");
        }

        private static bool TryHandleMessage(CSteamID remoteSteamID, BaseMessage message)
        {
            if (message == null)
            {
                RaftMMOLogger.LogWarning("RaftMMO: Got null message from: " + remoteSteamID);
                return false;
            }

            if (message.gameVersion != Settings.AppBuildID)
            {
                RaftMMOLogger.LogWarning("RaftMMO: Got message with wrong game version: " + message.gameVersion + ", expected: " + Settings.AppBuildID);
                return false;
            }

            if (message.modVersion != Globals.ModNetworkVersion)
            {
                RaftMMOLogger.LogWarning("RaftMMO: Got message with wrong mod network version: " + message.modVersion + ", expected: " + Globals.ModNetworkVersion);
                return false;
            }

            // Special handling for split messages
            if (message.type == MessageType.SPLIT_MESSAGE)
            {
                var completemessage = ProcessSplitMessage(remoteSteamID, message as SplitMessage, out bool success);
                if (success && completemessage != null)
                {
                    return TryHandleMessage(remoteSteamID, completemessage);
                }
                else
                {
                    return success;
                }
            }

            if (Raft_Network.IsHost)
            {
                ProcessHostMessage(remoteSteamID, message);
            }
            else if (ComponentManager<Raft_Network>.Value.IsConnectedToHost)
            {
                ProcessClientMessage(remoteSteamID, message);
            }

            return true;
        }

        private static Dictionary<ulong, List<SplitMessage>> splitmessageCache = new Dictionary<ulong, List<SplitMessage>>();



        private static BaseMessage ProcessSplitMessage(CSteamID steamID, SplitMessage splitMessage, out bool success)
        {
            RaftMMOLogger.LogVerbose("ProcessSplitMessage: " + splitMessage.id + ", " + splitMessage.index + ", " + splitMessage.totalnumber);

            if (splitMessage.index == 0)
            {
                if (splitmessageCache.ContainsKey(steamID.m_SteamID))
                {
                    RaftMMOLogger.LogWarning("ProcessSplitMessage: Received new split message before old split message finished, discarding old.");
                    splitmessageCache.Remove(steamID.m_SteamID);
                }
            }

            if (!splitmessageCache.ContainsKey(steamID.m_SteamID))
            {
                if (splitMessage.index != 0)
                {
                    RaftMMOLogger.LogWarning("ProcessSplitMessage: Received split message out of order, discarding: " + splitMessage.id + ", " + splitMessage.index + ", " + splitMessage.totalnumber);
                    success = false;
                    return null;
                }
                splitmessageCache.Add(steamID.m_SteamID, new List<SplitMessage>());
                splitmessageCache[steamID.m_SteamID].Add(splitMessage);
                success = false;
                return null;
            }

            var blub = splitmessageCache[steamID.m_SteamID].Where(s => s.id != splitMessage.id || s.totalnumber != splitMessage.totalnumber).Any();
            var bleb = splitmessageCache[steamID.m_SteamID].Count != splitMessage.index;
            if (blub || bleb)
            {
                RaftMMOLogger.LogWarning("ProcessSplitMessage: Invalid split message (" + blub + ", " + bleb + ", " + splitmessageCache[steamID.m_SteamID].Count + "), discarding: " + splitMessage.id + ", " + splitMessage.index + ", " + splitMessage.totalnumber);
                success = false;
                return null;
            }

            success = true;
            splitmessageCache[steamID.m_SteamID].Add(splitMessage);

            if (splitmessageCache[steamID.m_SteamID].Count == splitMessage.totalnumber)
            {
                var message = ReassembleSplitMessages(splitmessageCache[steamID.m_SteamID]);
                splitmessageCache.Remove(steamID.m_SteamID);
                return message;
            }

            return null;
        }

        private static BaseMessage ReassembleSplitMessages(List<SplitMessage> splitMessages)
        {
            RaftMMOLogger.LogVerbose("ReassembleSplitMessages: " + splitMessages.Count);

            MemoryStream memoryStream = new MemoryStream();
            foreach (var splitMessage in splitMessages)
            {
                memoryStream.Write(splitMessage.data, 0, splitMessage.data.Length);
            }
            memoryStream.Position = 0;

            RaftMMOLogger.LogVerbose("ReassembleSplitMessages Done: " + memoryStream.Length);

            return DeserializeMessage(memoryStream);
        }

        public static void RequestFullRaftUpdate()
        {
            RaftMMOLogger.LogVerbose("RequestFullRaftUpdate");

            if (Raft_Network.IsHost)
            {
                if (RemoteSession.IsConnectedToPlayer)
                {
                    SendMessage(RemoteSession.ConnectedPlayer, new BaseMessage(MessageType.REQUEST_FULL_RAFT, true));
                }
            }
            else if (ComponentManager<Raft_Network>.Value.IsConnectedToHost)
            {
                SendMessage(ComponentManager<Raft_Network>.Value.HostID, new BaseMessage(MessageType.REQUEST_FULL_RAFT, true));
            }

            RaftMMOLogger.LogVerbose("RequestFullRaftUpdate Done!");
        }

        private static void ProcessClientMessage(CSteamID steamID, BaseMessage message)
        {
            if (message == null)
                return;

            // Following messages are only accepted from our host
            if (ComponentManager<Raft_Network>.Value.HostID != steamID)
                return;

            if (message.type == MessageType.DISCONNECT)
            {
                ClientSession.HandleDisconnect();
                return;
            }

            if (message.type == MessageType.CONNECTED)
            {
                ClientSession.HandleConnectedMessage(message as ConnectedMessage);
                return;
            }

            if (message.type == MessageType.LIST_OF_PLAYERS)
            {
                RemoteRaft.SetListOfPlayers((message as PlayerListMessage).players);
                return;
            }

            if (message.type == MessageType.BUOYS)
            {
                ClientSession.HandleBuoyUpdate(message as BuoysUpdateMessage);
                return;
            }

            if (message.type == MessageType.TRADE)
            {
                TradeManager.HandleTradeMessage(message as TradeMessage);
                return;
            }

            if (message.type == MessageType.COMPLETE_TRADE)
            {
                TradeManager.HandleCompleteTradeMessage(message as CompleteTradeMessage);
                return;
            }

            if (message.type == MessageType.FULL_RAFT)
            {
                if ((message as RaftDeltaMessage).counter != 0)
                {
                    RaftMMOLogger.LogWarning("Invalid counter in RaftDeltaMessage (FULL_RAFT): " + (message as RaftDeltaMessage).counter);
                }
                RemoteRaft.SetLastUpdateCounter(0);

                ClientSession.HandleRaftPositionUpdate(message as IPositionUpdateMessage);
                RemoteRaft.ReplaceRaftParts((message as FullRaftMessage).added_data);
                if (ClientSession.IsHostConnectedToPlayer)
                {
                    SettingsManager.AddMetRaft(ClientSession.ConnectedSteamID, ClientSession.ConnectedSessionID, true);
                }
                return;
            }

            if (message.type == MessageType.RAFT_DELTA)
            {
                if ((message as RaftDeltaMessage).counter == 0)
                {
                    RaftMMOLogger.LogWarning("Invalid counter in RaftDeltaMessage (RAFT_DELTA): " + (message as RaftDeltaMessage).counter);
                }
                if (!RemoteRaft.SetLastUpdateCounter((message as RaftDeltaMessage).counter))
                    return;

                RemoteRaft.RemoveRaftParts((message as RaftDeltaMessage).removed_data);
                RemoteRaft.AddRaftParts((message as RaftDeltaMessage).added_data);
                if (ClientSession.IsHostConnectedToPlayer && !SettingsManager.HasMetRaft(ClientSession.ConnectedSteamID, ClientSession.ConnectedSessionID))
                {
                    SettingsManager.AddMetRaft(ClientSession.ConnectedSteamID, ClientSession.ConnectedSessionID, true);
                }
                return;
            }

            if (message.type == MessageType.RAFT_POS_UPDATE)
            {
                ClientSession.HandleRaftPositionUpdate(message as IPositionUpdateMessage);
                return;
            }

            if (message.type == MessageType.PLAYER_POS_UPDATE)
            {
                ClientSession.HandlePlayerPositionUpdate(message as PlayerUpdateMessage);
                return;
            }
        }

        private static void ProcessHostMessage(CSteamID steamID, BaseMessage message)
        {
            if (message == null)
                return;

            RaftMMOLogger.LogVerbose("ProcessHostMessage: " + message.type + ", " + RemoteSession.IsConnectedPlayer(steamID) + ", " + message.handshake);

            if (message.type == MessageType.REQUEST_CONNECTION)
            {
                RemoteSession.HandleRequestConnection(steamID, (message as RequestConnectionMessage).mySessionID, (message as RequestConnectionMessage).myHandshake);
                return;
            }

            if (message.type == MessageType.ACCEPT_CONNECTION)
            {
                RemoteSession.HandleAcceptConnection(steamID, (message as AcceptConnectionMessage).mySessionID, (message as AcceptConnectionMessage).yourHandshake, (message as AcceptConnectionMessage).myHandshake);
                return;
            }

            if (message.type == MessageType.REJECT_CONNECTION)
            {
                RemoteSession.HandleRejectConnection(steamID);
                return;
            }

            if (message.type == MessageType.REQUEST_FULL_RAFT)
            {
                if (RemoteSession.IsConnectedPlayer(steamID))
                {
                    Globals.FullRaftUpdateRequested = true;
                }
                else
                {
                    ClientSession.HandleFullRaftUpdateRequest(steamID);
                }
                return;
            }

            if (message.type == MessageType.TRADE)
            {
                TradeManager.HandleTradeMessage(message as TradeMessage);

                // Forward trade message from connected session to the client that's targeted
                if (RemoteSession.IsConnectedPlayer(steamID))
                {
                    foreach (var player in ClientSession.GetSessionPlayers().Where(p => SteamHelper.IsSameSteamID(p.steamID.m_SteamID, (message as TradeMessage).remoteTradePlayerSteamID)))
                    {
                        SendMessage(player.steamID, message);
                    }
                }
                // Forward trade message from our clients to connected session
                else
                {
                    SendMessage(RemoteSession.ConnectedPlayer, message);
                }

                return;
            }

            if (message.type == MessageType.COMPLETE_TRADE)
            {
                TradeManager.HandleCompleteTradeMessage(message as CompleteTradeMessage);

                // Forward complete trade message from connected session to the client that's targeted
                if (RemoteSession.IsConnectedPlayer(steamID))
                {
                    foreach (var player in ClientSession.GetSessionPlayers().Where(p => SteamHelper.IsSameSteamID(p.steamID.m_SteamID, (message as CompleteTradeMessage).remoteTradePlayerSteamID)))
                    {
                        SendMessage(player.steamID, message);
                    }
                }
                // Forward trade message from our clients to connected session
                else
                {
                    SendMessage(RemoteSession.ConnectedPlayer, message);
                }

                return;
            }

            // Following messages are only accepted from the steam ID we are currently connected to
            if (!RemoteSession.IsConnectedPlayer(steamID) || message.handshake != RemoteSession.LocalHandShake)
            {
                RaftMMOLogger.LogDebug("Unconnected Message: " + message.type
                    + ", IsConnectedPlayer: " + RemoteSession.IsConnectedPlayer(steamID)
                    + ", handshake received: " + message.handshake
                    + ", handshake expected: " + RemoteSession.LocalHandShake);
                return;
            }

            if (message.type == MessageType.DISCONNECT)
            {
                RemoteSession.HandleDisconnect(steamID);
                return;
            }

            if (message.type == MessageType.LIST_OF_PLAYERS)
            {
                RemoteRaft.SetListOfPlayers((message as PlayerListMessage).players);
                return;
            }

            if (message.type == MessageType.FULL_RAFT)
            {
                if ((message as RaftDeltaMessage).counter != 0)
                {
                    RaftMMOLogger.LogWarning("Invalid counter in RaftDeltaMessage (FULL_RAFT): " + (message as RaftDeltaMessage).counter);
                }
                RemoteRaft.SetLastUpdateCounter(0);

                RaftMMOLogger.LogDebug("MessageType.FULL_RAFT: " + (message as FullRaftMessage).counter);

                RaftMMOLogger.LogVerbose("MessageType.FULL_RAFT:\n"
                        + "Blocks received for removal: " + (message as RaftDeltaMessage).removed_data.blockData + "\n"
                        + "Blocks received for adding: " + (message as RaftDeltaMessage).added_data.blockData);

                RemoteSession.HandleRaftPositionUpdate(message as IPositionUpdateMessage);
                RemoteRaft.ReplaceRaftParts((message as FullRaftMessage).added_data);
                SettingsManager.AddMetRaft(steamID.m_SteamID, RemoteSession.ConnectedSessionID, true);
                return;
            }

            if (message.type == MessageType.RAFT_DELTA)
            {
                if ((message as RaftDeltaMessage).counter == 0)
                {
                    RaftMMOLogger.LogWarning("Invalid counter in RaftDeltaMessage (RAFT_DELTA): " + (message as RaftDeltaMessage).counter);
                }
                if (!RemoteRaft.SetLastUpdateCounter((message as RaftDeltaMessage).counter))
                    return;

                RaftMMOLogger.LogDebug("MessageType.RAFT_DELTA: " + (message as RaftDeltaMessage).counter);

                RaftMMOLogger.LogVerbose("MessageType.RAFT_DELTA:\n"
                        + "Blocks received for removal: " + (message as RaftDeltaMessage).removed_data.blockData + "\n"
                        + "Blocks received for adding: " + (message as RaftDeltaMessage).added_data.blockData);

                RemoteRaft.RemoveRaftParts((message as RaftDeltaMessage).removed_data);
                RemoteRaft.AddRaftParts((message as RaftDeltaMessage).added_data);
                return;
            }

            if (message.type == MessageType.RAFT_POS_UPDATE)
            {
                RemoteSession.HandleRaftPositionUpdate(message as IPositionUpdateMessage);
                return;
            }

            if (message.type == MessageType.PLAYER_POS_UPDATE)
            {
                RemoteSession.HandlePlayerPositionUpdate(message as PlayerUpdateMessage);
                return;
            }
        }
    }
}
