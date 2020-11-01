using RaftMMO.Utilities;
using RaftMMO.World;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class BuoysUpdateMessage : BaseMessage
    {
        private readonly SerializableData.Vector2D[] buoyLocations;
        private readonly SerializableData.Vector currentRaftMeetingPoint;
        private readonly float currentRaftMeetingPointDistance;
        public IEnumerable<Vector2> BuoyLocations { get { return buoyLocations.Select(l => l.Vector2); } }
        public Vector3 CurrentRaftMeetingPoint { get { return currentRaftMeetingPoint.Vector3; } }
        public float CurrentRaftMeetingPointDistance { get { return currentRaftMeetingPointDistance; } }
        public BuoysUpdateMessage()
            : base(MessageType.BUOYS, false)
        {
            this.buoyLocations = BuoyManager.BuoyLocations.ToArray();
            this.currentRaftMeetingPoint = new SerializableData.Vector(Globals.CurrentRaftMeetingPoint);
            this.currentRaftMeetingPointDistance = Globals.CurrentRaftMeetingPointDistance;
        }
    }
}
