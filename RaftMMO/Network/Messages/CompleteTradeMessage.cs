using System.Collections.Generic;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class CompleteTradeMessage : BaseMessage
    {
        public ulong steamID;
        public ulong remoteTradePlayerSteamID;
        public SerializableData.Item[] offerItems;
        public SerializableData.Item[] remoteItems;

        public CompleteTradeMessage(List<SerializableData.Item> offerItems, List<SerializableData.Item> remoteItems, ulong remoteTradePlayerSteamID)
            : base(MessageType.COMPLETE_TRADE, true)
        {
            this.steamID = ComponentManager<Raft_Network>.Value.LocalSteamID.m_SteamID;
            this.offerItems = offerItems.ToArray();
            this.remoteItems = remoteItems.ToArray();
            this.remoteTradePlayerSteamID = remoteTradePlayerSteamID;
        }

        // for serialization
        public CompleteTradeMessage() { }
    }
}
