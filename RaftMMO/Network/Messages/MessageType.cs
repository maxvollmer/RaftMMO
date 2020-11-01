
namespace RaftMMO.Network.Messages
{
    public enum MessageType
    {
        REQUEST_CONNECTION,
        ACCEPT_CONNECTION,
        REJECT_CONNECTION,
        REQUEST_FULL_RAFT,

        FULL_RAFT,
        RAFT_DELTA,

        LIST_OF_PLAYERS,

        RAFT_POS_UPDATE,
        PLAYER_POS_UPDATE,

        DISCONNECT,
        BUOYS,
        CONNECTED,
        TRADE,
        COMPLETE_TRADE,

        SPLIT_MESSAGE
    }
}
