
namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class SplitMessage : BaseMessage
    {
        public int id;
        public int index;
        public int totalnumber;
        public byte[] data;

        public SplitMessage(int id, int index, int totalnumber, byte[] data)
            : base(MessageType.SPLIT_MESSAGE, false)
        {
            this.id = id;
            this.index = index;
            this.totalnumber = totalnumber;
            this.data = data;
        }

        // for serialization
        public SplitMessage() { }
    }
}
