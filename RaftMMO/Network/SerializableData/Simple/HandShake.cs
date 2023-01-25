namespace RaftMMO.Network.SerializableData.Simple
{
    [System.Serializable()]
    public class HandShake
    {
        public int mine;
        public int theirs;

        public HandShake(int mine, int theirs)
        {
            this.mine = mine;
            this.theirs = theirs;
        }

        //for serialization
        public HandShake() { }
    }
}
