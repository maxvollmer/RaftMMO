using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class Vector2D
    {
        public readonly float x;
        public readonly float y;
        public Vector2 Vector2 { get { return new Vector2(x, y); } }
        public Vector2D(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }
    }
}
