using HarmonyLib;
using RaftMMO.RaftCopyTools;
using RaftMMO.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [Serializable()]
    public class RaftBlockData
    {
        public int itemIndex = 0;
        public int dpsType = 0;

        // Basic values
        public Vector position = new Vector(Vector3.zero);
        public Vector rotation = new Vector(Vector3.zero);
        public TileBitmaskType bitmaskType;
        public int bitmaskValue = 0;
        public RaftColliderData[] colliders = new RaftColliderData[0];
        public RaftPlantData[] plants = new RaftPlantData[0];

        // Colors for side A
        public float colorA_r = 0f;
        public float colorA_g = 0f;
        public float colorA_b = 0f;
        public float colorA_a = 0f;
        public float patternColorA_r = 0f;
        public float patternColorA_g = 0f;
        public float patternColorA_b = 0f;
        public float patternColorA_a = 0f;
        public uint patternIndexA = 0;
        public bool isMaskedA = false;
        public bool isMaskFlippedA = false;

        public bool HasColorA()
        {
            return colorA_r != 0f || colorA_g != 0f || colorA_b != 0f || colorA_a != 0f
                || patternColorA_r != 0f || patternColorA_g != 0f || patternColorA_b != 0f || patternColorA_a != 0f;
        }

        public bool HasColorB()
        {
            return colorB_r != 0f || colorB_r != 0f || colorB_b != 0f || colorB_a != 0f
                || patternColorB_r != 0f || patternColorB_r != 0f || patternColorB_b != 0f || patternColorB_a != 0f;
        }

        // Colors for side B
        public float colorB_r = 0f;
        public float colorB_g = 0f;
        public float colorB_b = 0f;
        public float colorB_a = 0f;
        public float patternColorB_r = 0f;
        public float patternColorB_g = 0f;
        public float patternColorB_b = 0f;
        public float patternColorB_a = 0f;
        public uint patternIndexB = 0;
        public bool isMaskedB = false;
        public bool isMaskFlippedB = false;

        // Paint settings
        public int paintSide = 0;
        public int decoPaintSelect = 0;

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

            MaterialPropertyBlock matPropBlock = Traverse.Create(block).Field("matPropBlock").GetValue<MaterialPropertyBlock>();
            if (matPropBlock != null)
            {
                Color colorA = matPropBlock.GetColor("_Side1_Base");
                this.colorA_r = colorA.r;
                this.colorA_g = colorA.g;
                this.colorA_b = colorA.b;
                this.colorA_a = colorA.a;
                Color patternColorA = matPropBlock.GetColor("_Side1_Pattern");
                this.patternColorA_r = patternColorA.r;
                this.patternColorA_g = patternColorA.g;
                this.patternColorA_b = patternColorA.b;
                this.patternColorA_a = patternColorA.a;
                this.patternIndexA = (uint)matPropBlock.GetFloat("_Pattern_Index1");
                this.isMaskedA = matPropBlock.GetFloat("_Pattern1_Masked") == 1f;
                this.isMaskFlippedA = (uint)matPropBlock.GetFloat("_Pattern1_MaskFlip") == 1f;

                Color colorB = matPropBlock.GetColor("_Side2_Base");
                this.colorB_r = colorB.r;
                this.colorB_g = colorB.g;
                this.colorB_b = colorB.b;
                this.colorB_a = colorB.a;
                Color patternColorB = matPropBlock.GetColor("_Side2_Pattern");
                this.patternColorB_r = patternColorB.r;
                this.patternColorB_g = patternColorB.g;
                this.patternColorB_b = patternColorB.b;
                this.patternColorB_a = patternColorB.a;
                this.patternIndexB = (uint)matPropBlock.GetFloat("_Pattern_Index2");
                this.isMaskedB = matPropBlock.GetFloat("_Pattern2_Masked") == 1f;
                this.isMaskFlippedB = (uint)matPropBlock.GetFloat("_Pattern2_MaskFlip") == 1f;

                this.paintSide = (int)matPropBlock.GetFloat("_PaintSide");
                this.decoPaintSelect = (int)matPropBlock.GetFloat("_DecoPaintSelect");
            }

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

            this.colliders = block.GetComponentsInChildren<Collider>().Where(collider => RaftCopier.IsColliderForSending(block, collider)).Select(collider => new RaftColliderData(collider)).ToArray();
            this.plants = block.GetComponentsInChildren<Plant>().Select(plant => new RaftPlantData(plant)).ToArray();
        }

        // for serialization
        public RaftBlockData() { }

        public override bool Equals(object obj)
        {
            if (obj is RaftBlockData raftBlockData)
            {
                return itemIndex == raftBlockData.itemIndex
                    && dpsType == raftBlockData.dpsType
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
                    ^ HashPlants() << 7
                    ^ HashColliders() << 8;
                hasHash = true;
            }
            return hash;
        }

        public override string ToString()
        {
            return "{"
                + "itemIndex: " + itemIndex + ","
                + "dpsType: " + dpsType + ","
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
