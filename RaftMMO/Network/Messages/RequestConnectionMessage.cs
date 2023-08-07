
using RaftMMO.ModSettings;
using RaftMMO.Utilities;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class RequestConnectionMessage : BaseMessage
    {
        public string mySessionID;
        public int myHandshake;

        public RequestConnectionMessage()
          : base(MessageType.REQUEST_CONNECTION, true)
        {
            mySessionID = ((((ulong)SaveAndLoad.CurrentGameFileName.GetHashCode()) << 32) + (ulong)(SaveAndLoad.CurrentGameFileName + "a").GetHashCode()).ToString();
            myHandshake = RemoteSession.LocalHandShake;
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("RequestConnectionMessage(" + myHandshake + ")");
            }
        }
    }
}
