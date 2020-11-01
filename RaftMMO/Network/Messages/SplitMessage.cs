
namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class SplitMessage : BaseMessage
    {
        public readonly int id;
        public readonly int index;
        public readonly int totalnumber;
        public readonly byte[] data;

        public SplitMessage(int id, int index, int totalnumber, byte[] data)
            : base(MessageType.SPLIT_MESSAGE, false)
        {
            this.id = id;
            this.index = index;
            this.totalnumber = totalnumber;
            this.data = data;
        }
    }
}
