
using RaftMMO.Utilities;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class AcceptConnectionMessage : BaseMessage
    {
        public string mySessionID;
        public int yourHandshake;
        public int myHandshake;

        public AcceptConnectionMessage(int handshake)
          : base(MessageType.ACCEPT_CONNECTION, true)
        {
            mySessionID = ((((ulong)SaveAndLoad.CurrentGameFileName.GetHashCode()) << 32) + (ulong)(SaveAndLoad.CurrentGameFileName + "a").GetHashCode()).ToString();
            yourHandshake = handshake;
            myHandshake = RemoteSession.LocalHandShake;
            RaftMMOLogger.LogVerbose("AcceptConnectionMessage(" + yourHandshake + "," + myHandshake + ")");
        }

        // for serialization
        public AcceptConnectionMessage() { }
    }
}
