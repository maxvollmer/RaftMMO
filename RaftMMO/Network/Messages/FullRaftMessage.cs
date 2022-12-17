using UnityEngine;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class FullRaftMessage : RaftDeltaMessage, IPositionUpdateMessage
    {
        public SerializableData.Vector position;
        public SerializableData.Angles rotation;
        public float remotePosRotation;
        public SerializableData.Vector Position { get { return position; } }
        public SerializableData.Angles Rotation { get { return rotation; } }
        public float RemotePosRotation { get { return remotePosRotation; } }

        public FullRaftMessage(Vector3 position, Quaternion rotation, float remotePosRotation)
            : base(true)
        {
            this.position = new SerializableData.Vector(position);
            this.rotation = new SerializableData.Angles(rotation);
            this.remotePosRotation = remotePosRotation;
        }

        // for serialization
        public FullRaftMessage() { }
    }
}
