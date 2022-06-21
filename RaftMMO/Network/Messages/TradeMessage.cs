using System.Collections.Generic;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class TradeMessage : BaseMessage
    {
        public readonly ulong steamID;
        public readonly ulong remoteTradePlayerSteamID;
        public readonly SerializableData.Item[] offerItems;
        public readonly SerializableData.Item[] wishItems;
        public readonly bool isAcceptingTrade;

        public TradeMessage(List<SerializableData.Item> offerItems, List<SerializableData.Item> wishItems, ulong remoteTradePlayerSteamID, bool isAcceptingTrade)
            : base(MessageType.TRADE, true)
        {
            this.steamID = ComponentManager<Raft_Network>.Value.LocalSteamID.m_SteamID;
            this.offerItems = offerItems.ToArray();
            this.wishItems = wishItems.ToArray();
            this.remoteTradePlayerSteamID = remoteTradePlayerSteamID;
            this.isAcceptingTrade = isAcceptingTrade;
        }
    }
}
