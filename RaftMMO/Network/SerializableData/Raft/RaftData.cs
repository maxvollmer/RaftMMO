namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class RaftData
    {
        public readonly RaftBlockData[] blockData;

        public RaftData(RaftBlockData[] blockData)
        {
            this.blockData = blockData;
        }
    }
}