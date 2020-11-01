using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RaftMMO.World
{
    public class ChunkAndBuoyCombiner
    {
        [HarmonyPatch(typeof(ChunkManager), "DoesPointFit", typeof(ChunkPoint))]
        public class ChunkManagerDoesPointFitPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(ref bool __result, ChunkPoint pointToCheck)
            {
                if (pointToCheck == null)
                    return false;

                var pointWorldPosition = pointToCheck.worldPosition;
                var pointCollisionOverlapRadius = pointToCheck.rule.CollisionOverlapRadius;
                foreach (var buoyLocation in BuoyManager.BuoyLocations)
                {
                    var buoyWorldPosition = new Vector3(buoyLocation.x, 0f, buoyLocation.y);

                    var distance = pointWorldPosition.DistanceXZ(buoyWorldPosition);
                    var minDistance = (pointCollisionOverlapRadius + BuoyManager.BuoyCollisionOverlapRadius);

                    bool useMinDistance = Traverse.Create(pointToCheck.rule).Field("useMinDistance").GetValue<bool>();
                    float minDistanceToOthers = Traverse.Create(pointToCheck.rule).Field("minDistanceToOthers").GetValue<float>();

                    if (distance <= minDistance || (useMinDistance && distance < minDistanceToOthers))
                    {
                        __result = false;
                        return false;
                    }
                }

                return true;
            }
        }

        public static bool DoesBuoyFit(Vector2 position)
        {
            var chunkManager = ComponentManager<ChunkManager>.Value;
            Vector3 buoyWorldPosition = new Vector3(position.x, 0f, position.y);
            foreach (var pointToCompare in chunkManager.GetAllChunkPointsList())
            {
                if (pointToCompare != null)
                {
                    double distance = buoyWorldPosition.DistanceXZ(pointToCompare.worldPosition);
                    double minDistance = BuoyManager.BuoyCollisionOverlapRadius + pointToCompare.rule.CollisionOverlapRadius;
                    if (distance <= minDistance)
                        return false;
                }
            }
            return true;
        }
    }
}
