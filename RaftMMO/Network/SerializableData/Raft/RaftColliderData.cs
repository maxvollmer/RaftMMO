using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [Serializable()]
    public class RaftColliderData
    {
        [Serializable()]
        public enum ColliderType
        {
            INVALID,
            BOX,
            SPHERE,
            PIPEMESH,
            MESH
        }

        public readonly ColliderType type = ColliderType.INVALID;
        public readonly Vector position = new Vector(Vector3.zero);
        public readonly Vector rotation = new Vector(Vector3.zero);
        public readonly Vector scale = new Vector(Vector3.zero);
        public readonly Vector center = new Vector(Vector3.zero);
        public readonly Vector size = new Vector(Vector3.zero);
        public readonly int bitMaskValue = 0;
        public readonly bool isLadder = false;

        [NonSerialized()]
        private int hash = 0;

        [NonSerialized()]
        private bool hasHash = false;

        public RaftColliderData(Collider collider)
        {
            position = new Vector(collider.transform.position);
            rotation = new Vector(collider.transform.rotation.eulerAngles);
            scale = new Vector(ClampAndNormalizeScale(collider.transform.lossyScale));
            isLadder = false;

            if (collider is BoxCollider boxCollider)
            {
                type = ColliderType.BOX;
                center = new Vector(boxCollider.center);
                size = new Vector(boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                type = ColliderType.BOX;
                center = new Vector(sphereCollider.center);
                size = new Vector(new Vector3(sphereCollider.radius, sphereCollider.radius, sphereCollider.radius));
            }
            else if (collider is MeshCollider meshCollider)
            {
                var bitmaskTile = meshCollider.GetComponentInParent<Block>().GetComponent<BitmaskTile>();
                if (bitmaskTile != null
                    && (bitmaskTile.BitmaskType == TileBitmaskType.Pipe || bitmaskTile.BitmaskType == TileBitmaskType.Pipe_Water))
                {
                    type = ColliderType.PIPEMESH;
                    bitMaskValue = bitmaskTile.currentBitmaskValue;
                }
                else
                {
                    type = ColliderType.MESH;
                }
            }
            else
            {
                type = ColliderType.INVALID;
            }

            if (collider.isTrigger)
            {
                if (collider.tag == "Ladder")
                {
                    isLadder = true;
                }
                else
                {
                    type = ColliderType.INVALID;
                }
            }
        }

        private Vector3 ClampAndNormalizeScale(Vector3 scale)
        {
            int scaleX = (int)Math.Round(Math.Min(Math.Max(scale.x, 0f), 1f) * 10);
            int scaleY = (int)Math.Round(Math.Min(Math.Max(scale.y, 0f), 1f) * 10);
            int scaleZ = (int)Math.Round(Math.Min(Math.Max(scale.z, 0f), 1f) * 10);
            return new Vector3(scaleX * 0.1f, scaleY * 0.1f, scaleZ * 0.1f);
        }

        public override bool Equals(object obj)
        {
            if (obj is RaftColliderData raftColliderData)
            {
                return type == raftColliderData.type
                    && bitMaskValue == raftColliderData.bitMaskValue
                    && isLadder == raftColliderData.isLadder
                   // && position.Vector3 == raftColliderData.position.Vector3
                   // && rotation.Vector3 == raftColliderData.rotation.Vector3
                    && scale.Vector3 == raftColliderData.scale.Vector3
                    && center.Vector3 == raftColliderData.center.Vector3
                    && size.Vector3 == raftColliderData.size.Vector3
                   ;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (!hasHash)
            {
                // not the nicest implementation, but we don't really worry too much about collisions
                hash =
                isLadder.GetHashCode()
                ^ ((int)type) << 2
                ^ bitMaskValue << 4 
               // ^ position.Vector3.GetHashCode() << 2
               // ^ rotation.Vector3.GetHashCode() << 3
                ^ scale.Vector3.GetHashCode() << 4
                ^ center.Vector3.GetHashCode() >> 2
                ^ size.Vector3.GetHashCode() >> 3
               ;
                hasHash = true;
            }
            return hash;
        }

        public override string ToString()
        {
            return "{"
                + "type: " + type + ","
                + "bitMaskValue: " + bitMaskValue + ","
                + "isLadder: " + isLadder + ","
                + "scale: " + scale.Vector3 + ","
                + "center: " + center.Vector3 + ","
                + "size: " + size.Vector3
                + "}";
        }
    }
}
