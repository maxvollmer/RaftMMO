using RaftMMO.Utilities;
using RaftMMO.World;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class PlayerUpdateMessage : BaseMessage
    {
        public ulong steamID;
        public byte model;
        public SerializableData.Vector position;
        public byte rotationX;
        public byte rotationY;
        public RaftAttachStatus raftAttachStatus;
        public SerializableData.MessagePlayerUpdateClone playerUpdate;

        public float RotationX { get { return 360f * (rotationX / 255f); } }
        public float RotationY { get { return 360f * (rotationY / 255f); } }

        public PlayerUpdateMessage(Network_Player player, RaftAttachStatus raftAttachStatus)
            : base(MessageType.PLAYER_POS_UPDATE, false)
        {
            var raft = ComponentManager<Raft>.Value;
            this.steamID = player.steamID.m_SteamID;
            this.model = (byte)player.characterSettings.ModelIndex;
            var posTranslated = player.transform.position - Globals.CurrentRaftMeetingPoint;
            this.position = new SerializableData.Vector(posTranslated);
            this.rotationX = (byte)(255 * (player.transform.rotation.eulerAngles.x / 360f));
            this.rotationY = (byte)(255 * (player.transform.rotation.eulerAngles.y / 360f));
            this.raftAttachStatus = raftAttachStatus;
            this.playerUpdate = new SerializableData.MessagePlayerUpdateClone(global::Messages.Update, player, player);
        }

        // for serialization
        public PlayerUpdateMessage() { }
    }
}
