using System.Collections.Generic;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class CompleteTradeMessage : BaseMessage
    {
        public readonly ulong steamID;
        public readonly ulong remoteTradePlayerSteamID;
        public readonly SerializableData.Item[] offerItems;
        public readonly SerializableData.Item[] remoteItems;

        public CompleteTradeMessage(List<SerializableData.Item> offerItems, List<SerializableData.Item> remoteItems, ulong remoteTradePlayerSteamID)
            : base(MessageType.COMPLETE_TRADE, true)
        {
            this.steamID = ComponentManager<Semih_Network>.Value.LocalSteamID.m_SteamID;
            this.offerItems = offerItems.ToArray();
            this.remoteItems = remoteItems.ToArray();
            this.remoteTradePlayerSteamID = remoteTradePlayerSteamID;
        }
    }
}
