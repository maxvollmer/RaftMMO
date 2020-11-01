using System.Collections.Generic;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.Trade
{
    public class PlayerTradeData
    {
        public List<SerializableData.Item> OfferItems { get; private set; }
        public List<SerializableData.Item> WishItems { get; private set; }
        public ulong RemoteTradePlayerSteamID { get; private set; }
        public bool IsAcceptingTrade { get; private set; }

        public PlayerTradeData(List<SerializableData.Item> offerItems, List<SerializableData.Item> wishItems, ulong remoteTradePlayerSteamID, bool isAcceptingTrade)
        {
            OfferItems = offerItems;
            WishItems = wishItems;
            RemoteTradePlayerSteamID = remoteTradePlayerSteamID;
            IsAcceptingTrade = isAcceptingTrade;
        }
    }
}
