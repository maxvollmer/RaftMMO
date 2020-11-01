
namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class ConnectedMessage : BaseMessage
    {
        public readonly bool IsConnectedToPlayer;
        public readonly ulong ConnectedSessionSteamID;
        public readonly string ConnectedSessionID;

        public ConnectedMessage()
            : base(MessageType.CONNECTED, true)
        {
            IsConnectedToPlayer = RemoteSession.IsConnectedToPlayer;
            ConnectedSessionSteamID = IsConnectedToPlayer ? RemoteSession.ConnectedPlayer.m_SteamID : 0;
            ConnectedSessionID = RemoteSession.ConnectedSessionID;
        }
    }
}
