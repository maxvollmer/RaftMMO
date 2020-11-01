namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class MessagePlayerUpdateWrapper : Message_Player_Update
    {
        public MessagePlayerUpdateWrapper(global::Messages type, MonoBehaviour_Network behaviour, Network_Player player)
          : base(type, behaviour, player)
        {
        }
    }
}
