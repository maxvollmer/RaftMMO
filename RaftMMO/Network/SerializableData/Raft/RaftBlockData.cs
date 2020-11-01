using RaftMMO.RaftCopyTools;
using RaftMMO.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [Serializable()]
    public class RaftBlockData
    {
        public readonly int itemIndex = 0;
        public readonly int dpsType = 0;
        public readonly uint uniqueColorIndex = 0;
        public readonly Vector position = new Vector(Vector3.zero);
        public readonly Vector rotation = new Vector(Vector3.zero);
        public readonly TileBitmaskType bitmaskType;
        public readonly int bitmaskValue = 0;
        public readonly RaftColliderData[] colliders = new RaftColliderData[0];
        public readonly RaftPlantData[] plants = new RaftPlantData[0];

        [NonSerialized()]
        private int hash = 0;

        [NonSerialized()]
        private bool hasHash = false;

        public RaftBlockData(Block block)
        {
            this.itemIndex = block.buildableItem.UniqueIndex;
            this.dpsType = (int)block.dpsType;
            this.position = new Vector(block.transform.localPosition);
            this.rotation = new Vector(block.transform.localRotation.eulerAngles);
            this.uniqueColorIndex = block.GetUniqueColorIndex();

            var bitmaskTile = block.GetComponent<BitmaskTile>();
            if (bitmaskTile != null)
            {
                this.bitmaskType = bitmaskTile.BitmaskType;
                this.bitmaskValue = bitmaskTile.currentBitmaskValue;
            }
            else
            {
                this.bitmaskType = TileBitmaskType.All;
                this.bitmaskValue = 0;
            }

            colliders = block.GetComponentsInChildren<Collider>().Where(RaftCopier.IsColliderForSending).Select(collider => new RaftColliderData(collider)).ToArray();
            plants = block.GetComponentsInChildren<Plant>().Select(plant => new RaftPlantData(plant)).ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj is RaftBlockData raftBlockData)
            {
                return itemIndex == raftBlockData.itemIndex
                    && dpsType == raftBlockData.dpsType
                    && uniqueColorIndex == raftBlockData.uniqueColorIndex
                    && position.Vector3 == raftBlockData.position.Vector3
                    && rotation.Vector3 == raftBlockData.rotation.Vector3
                    && bitmaskType == raftBlockData.bitmaskType
                    && bitmaskValue == raftBlockData.bitmaskValue
                    && CompareColliders(raftBlockData)
                    && ComparePlants(raftBlockData);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (!hasHash)
            {
                // not the nicest implementation, but we don't really worry too much about collisions
                hash =
                    itemIndex
                    ^ position.Vector3.GetHashCode() << 2
                    ^ rotation.Vector3.GetHashCode() << 3
                    ^ ((int)bitmaskType) << 4
                    ^ bitmaskValue << 5
                    ^ dpsType << 6
                    ^ uniqueColorIndex.GetHashCode() << 7
                    ^ HashPlants() << 8
                    ^ HashColliders() << 9;
                hasHash = true;
            }
            return hash;
        }

        public override string ToString()
        {
            return "{"
                + "itemIndex: " + itemIndex + ","
                + "dpsType: " + dpsType + ","
                + "uniqueColorIndex: " + uniqueColorIndex + ","
                + "position: " + position.Vector3 + ","
                + "rotation: " + rotation.Vector3 + ","
                + "bitmaskType: " + bitmaskType + ","
                + "bitmaskValue: " + bitmaskValue + ","
                + "colliders: " + GameObjectDebugger.DebugPrint(colliders) + ","
                + "plants: " + GameObjectDebugger.DebugPrint(plants)
                + "}";
        }

        private bool ComparePlants(RaftBlockData raftBlockData)
        {
            if (plants.Length != raftBlockData.plants.Length)
                return false;

            for (var i = 0; i < plants.Length; i++)
            {
                if (!plants[i].Equals(raftBlockData.plants[i]))
                    return false;
            }

            return true;
        }

        private bool CompareColliders(RaftBlockData raftBlockData)
        {
            if (colliders.Length != raftBlockData.colliders.Length)
                return false;

            for (var i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].Equals(raftBlockData.colliders[i]))
                    return false;
            }

            return true;
        }

        private int HashPlants()
        {
            if (plants.Length == 0)
                return 1337;

            int hash = plants[0].GetHashCode();
            for (var i = 1; i < plants.Length; i++)
            {
                hash ^= plants[i].GetHashCode() << i;
            }
            return hash;
        }

        private int HashColliders()
        {
            if (colliders.Length == 0)
                return 1337;

            int hash = colliders[0].GetHashCode();
            for (var i = 1; i < colliders.Length; i++)
            {
                hash ^= colliders[i].GetHashCode() << i;
            }
            return hash;
        }
    }
}
