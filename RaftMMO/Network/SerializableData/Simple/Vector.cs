using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class Vector
    {
        public float x;
        public float y;
        public float z;
        public Vector3 Vector3 { get { return new Vector3(x, y, z); } }

        public Vector(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        // for serialization
        public Vector() { }
    }
}
