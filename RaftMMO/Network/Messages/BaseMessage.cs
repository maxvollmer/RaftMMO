using RaftMMO.Network.SerializableData.Simple;
using RaftMMO.Utilities;
using System;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class BaseMessage
    {
        public readonly MessageType type;
        public readonly int gameVersion;
        public readonly int modVersion;
        public readonly int handshake;

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
    }
}
