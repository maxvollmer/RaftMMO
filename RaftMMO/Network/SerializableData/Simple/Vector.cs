using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class Vector
    {
        public readonly float x;
        public readonly float y;
        public readonly float z;
        public Vector3 Vector3 { get { return new Vector3(x, y, z); } }
        public Vector(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    }
}
