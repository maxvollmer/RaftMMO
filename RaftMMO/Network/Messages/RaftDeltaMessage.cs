
namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class RaftDeltaMessage : BaseMessage
    {
        public SerializableData.RaftData added_data = null;
        public SerializableData.RaftData removed_data = null;
        public int counter = 0;

        public RaftDeltaMessage()
            : this(false)
        {
        }

        protected RaftDeltaMessage(bool full)
            : base(full ? MessageType.FULL_RAFT : MessageType.RAFT_DELTA, true)
        {
        }
    }
}
