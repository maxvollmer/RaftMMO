
using RaftMMO.Utilities;
using System;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class RequestConnectionMessage : BaseMessage
    {
        public readonly string mySessionID;
        public readonly int myHandshake;

        public RequestConnectionMessage()
          : base(MessageType.REQUEST_CONNECTION, true)
        {
            mySessionID = ((((ulong)SaveAndLoad.CurrentGameFileName.GetHashCode()) << 32) + (ulong)(SaveAndLoad.CurrentGameFileName + "a").GetHashCode()).ToString();
            myHandshake = RemoteSession.LocalHandShake;
            RaftMMOLogger.LogVerbose("RequestConnectionMessage(" + myHandshake + ")");
        }
    }
}
