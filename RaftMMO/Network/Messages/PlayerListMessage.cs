namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class PlayerListMessage : BaseMessage
    {
        public ulong[] players;

        public PlayerListMessage(ulong[] players)
            : base(MessageType.LIST_OF_PLAYERS, true)
        {
            this.players = players;
        }

        // for serialization
        public PlayerListMessage() { }
    }
}
