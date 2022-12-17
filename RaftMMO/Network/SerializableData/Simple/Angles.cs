using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class Angles
    {
        private byte x;
        private byte y;
        private byte z;

        public float X { get { return x / 255f * 360f; } }
        public float Y { get { return y / 255f * 360f; } }
        public float Z { get { return z / 255f * 360f; } }
        public Quaternion Quaternion { get { return Quaternion.Euler(X, Y, Z); } }

        public Angles(Quaternion quaternion)
        {
            x = (byte)(quaternion.eulerAngles.x / 360f * 255f);
            y = (byte)(quaternion.eulerAngles.y / 360f * 255f);
            z = (byte)(quaternion.eulerAngles.z / 360f * 255f);
        }

        // for serialization
        public Angles() { }
    }
}
