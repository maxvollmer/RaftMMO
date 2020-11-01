using RaftMMO.Network.SerializableData.Simple;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class DisconnectMessage : BaseMessage
    {
        public DisconnectMessage(int? overridehandshake)
          : base(MessageType.DISCONNECT, true, overridehandshake)
        {
        }
    }
}
