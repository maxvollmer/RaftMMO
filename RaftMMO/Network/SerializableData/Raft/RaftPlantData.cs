using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaftMMO.Network.SerializableData
{
    [Serializable()]
    public class RaftPlantData
    {
        public Vector position = new Vector(Vector3.zero);
        public Vector rotation = new Vector(Vector3.zero);
        public Vector scale = new Vector(Vector3.zero);
        public int plantUniqueItemIndex = 0;

        [NonSerialized()]
        private int hash = 0;

        [NonSerialized()]
        private bool hasHash = false;

        public RaftPlantData(Plant plant)
        {
            position = new Vector(plant.transform.position);
            rotation = new Vector(plant.transform.rotation.eulerAngles);
            scale = new Vector(ClampAndNormalizeScale(plant.transform.lossyScale));
            plantUniqueItemIndex = plant.item.UniqueIndex;
        }

        // for serialization
        public RaftPlantData() { }

        private Vector3 ClampAndNormalizeScale(Vector3 scale)
        {
            int scaleX = (int)Math.Round(Math.Min(Math.Max(scale.x, 0f), 1f) * 10);
            int scaleY = (int)Math.Round(Math.Min(Math.Max(scale.y, 0f), 1f) * 10);
            int scaleZ = (int)Math.Round(Math.Min(Math.Max(scale.z, 0f), 1f) * 10);
            return new Vector3(scaleX * 0.1f, scaleY * 0.1f, scaleZ * 0.1f);
        }

        public override bool Equals(object obj)
        {
            if (obj is RaftPlantData raftPlantData)
            {
                return plantUniqueItemIndex == raftPlantData.plantUniqueItemIndex
                    && scale.Vector3 == raftPlantData.scale.Vector3;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (!hasHash)
            {
                // not the nicest implementation, but we don't really worry too much about collisions
                hash = plantUniqueItemIndex ^ scale.Vector3.GetHashCode() << 2;
                hasHash = true;
            }
            return hash;
        }

        public override string ToString()
        {
            return "{"
                + "plantUniqueItemIndex: " + plantUniqueItemIndex + ","
                + "scale: " + scale.Vector3
                + "}";
        }
    }
}
