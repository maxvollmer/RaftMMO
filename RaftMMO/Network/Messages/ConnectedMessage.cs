
namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class ConnectedMessage : BaseMessage
    {
        public bool IsConnectedToPlayer;
        public ulong ConnectedSessionSteamID;
        public string ConnectedSessionID;

        public ConnectedMessage()
            : base(MessageType.CONNECTED, true)
        {
            IsConnectedToPlayer = RemoteSession.IsConnectedToPlayer;
            ConnectedSessionSteamID = IsConnectedToPlayer ? RemoteSession.ConnectedPlayer.m_SteamID : 0;
            ConnectedSessionID = RemoteSession.ConnectedSessionID;
        }
    }
}
