namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class RaftData
    {
        public RaftBlockData[] blockData;

        public RaftData(RaftBlockData[] blockData)
        {
            this.blockData = blockData;
        }

        // for serialization
        public RaftData() { }
    }
}