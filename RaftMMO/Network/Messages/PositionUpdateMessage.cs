﻿using UnityEngine;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class PositionUpdateMessage : BaseMessage, IPositionUpdateMessage
    {
        public readonly SerializableData.Vector position;
        public readonly SerializableData.Angles rotation;
        public readonly float remotePosRotation;

        public SerializableData.Vector Position { get { return position; } }
        public SerializableData.Angles Rotation { get { return rotation; } }
        public float RemotePosRotation { get { return remotePosRotation; } }

        public PositionUpdateMessage(Vector3 position, Quaternion rotation, float remotePosRotation)
            : base(MessageType.RAFT_POS_UPDATE, false)
        {
            this.position = new SerializableData.Vector(position);
            this.rotation = new SerializableData.Angles(rotation);
            this.remotePosRotation = remotePosRotation;
        }
    }
}
