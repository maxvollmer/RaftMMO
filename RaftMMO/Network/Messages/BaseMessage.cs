using RaftMMO.Utilities;
using System;
using System.Xml.Serialization;

namespace RaftMMO.Network.Messages
{
    [Serializable(),
        XmlInclude(typeof(AcceptConnectionMessage)),
        XmlInclude(typeof(BuoysUpdateMessage)),
        XmlInclude(typeof(CompleteTradeMessage)),
        XmlInclude(typeof(ConnectedMessage)),
        XmlInclude(typeof(DisconnectMessage)),
        XmlInclude(typeof(FullRaftMessage)),
        XmlInclude(typeof(PlayerListMessage)),
        XmlInclude(typeof(PlayerUpdateMessage)),
        XmlInclude(typeof(PositionUpdateMessage)),
        XmlInclude(typeof(RaftDeltaMessage)),
        XmlInclude(typeof(RequestConnectionMessage)),
        XmlInclude(typeof(SplitMessage)),
        XmlInclude(typeof(TradeMessage))]
    public class BaseMessage
    {
        public MessageType type;
        public int gameVersion;
        public int modVersion;
        public int handshake;

        [System.NonSerialized()]
        public readonly bool reliable;

        public BaseMessage(MessageType type, bool reliable, int? overridehandshake = null)
        {
            this.type = type;
            this.reliable = reliable;
            this.gameVersion = Settings.AppBuildID;
            this.modVersion = Globals.ModNetworkVersion;
            this.handshake = overridehandshake.HasValue ? overridehandshake.Value : RemoteSession.RemoteHandShake;
        }

        // for serialization
        public BaseMessage() { }
    }
}
