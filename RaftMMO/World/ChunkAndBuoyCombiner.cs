using HarmonyLib;
using RaftMMO.Network;
using RaftMMO.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMMO.World
{
    public class ChunkAndBuoyCombiner
    {
        private static bool IsTooClose(ChunkPoint pointToCheck, Vector2 buoyLocation)
        {
            bool isTooClose = false;

            var pointWorldPosition = pointToCheck.worldPosition;
            var buoyWorldPosition = new Vector3(buoyLocation.x, 0f, buoyLocation.y);
            var distance = pointWorldPosition.DistanceXZ(buoyWorldPosition);

            if (distance <= pointToCheck.rule.CollisionOverlapRadius)
            {
                isTooClose = true;
            }

            bool useMinDistance = Traverse.Create(pointToCheck.rule).Field("useMinDistance").GetValue<bool>();
            if (useMinDistance)
            {
                float minDistanceToOthers = Traverse.Create(pointToCheck.rule).Field("minDistanceToOthers").GetValue<float>();
                if (distance < minDistanceToOthers)
                {
                    isTooClose = true;
                }
            }

            return isTooClose;
        }

        [HarmonyPatch(typeof(ChunkManager), "DoesPointFit", typeof(ChunkPoint))]
        public class ChunkManagerDoesPointFitPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(ref bool __result, ChunkPoint pointToCheck)
            {
                if (pointToCheck == null || pointToCheck.rule == null)
                    return false;

                foreach (var buoyLocation in BuoyManager.VisibleBuoyLocations)
                {
                    if (IsTooClose(pointToCheck, buoyLocation.Vector2))
                    {
                        __result = false;
                        return false;
                    }
                }

                return true;
            }
        }

        private static IEnumerable<ChunkPoint> GetAllChunkPoints()
        {
            var chunkManager = ComponentManager<ChunkManager>.Value;
            return chunkManager.GetAllChunkPointsList().Where(p => p != null && !p.IsCorrupt);
        }


        [HarmonyPatch(typeof(ChunkManager), "Update")]
        public class ChunkManagerUpdatePatch
        {
            private struct CatapultedChunkpoint
            {
                public ChunkPoint chunkPoint;
                public Vector3 worldPosition;
                public float removeDistanceFromRaft;
            }
            private static List<CatapultedChunkpoint> catapultedChunkpoints = new List<CatapultedChunkpoint>();

            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix()
            {
                if (Raft_Network.IsHost && RemoteSession.IsConnectedToPlayer)
                {
                    // Catapult all chunk points too close to an active buoy into space 
                    foreach (var pointToCheck in GetAllChunkPoints().Where(p => !p.HasSpawnedObject))
                    {
                        if (IsTooClose(pointToCheck, new Vector2(Globals.CurrentRaftMeetingPoint.x, Globals.CurrentRaftMeetingPoint.z)))
                        {
                            catapultedChunkpoints.Add(new CatapultedChunkpoint()
                            {
                                chunkPoint = pointToCheck,
                                worldPosition = pointToCheck.worldPosition,
                                removeDistanceFromRaft = pointToCheck.RemoveDistanceFromRaft
                            });
                            pointToCheck.worldPosition = new Vector3(999999999999.0f, 999999999999.0f, 999999999999.0f);
                            pointToCheck.RemoveDistanceFromRaft = 0;
                        }
                    }
                }
                return true;
            }

            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix()
            {
                // Return all chunk points catapulted in Prefix()
                foreach (var catapultedChunkpoint in catapultedChunkpoints)
                {
                    catapultedChunkpoint.chunkPoint.worldPosition = catapultedChunkpoint.worldPosition;
                    catapultedChunkpoint.chunkPoint.RemoveDistanceFromRaft = catapultedChunkpoint.removeDistanceFromRaft;
                }
                catapultedChunkpoints.Clear();
            }
        }

        public static bool DoesBuoyFit(Vector2 buoyLocation, out int numIslandsInWay)
        {
            numIslandsInWay = 0;
            bool result = true;
            foreach (var pointToCheck in GetAllChunkPoints().Where(p => p.HasSpawnedObject))
            {
                if (IsTooClose(pointToCheck, buoyLocation))
                {
                    numIslandsInWay++;
                    result = false;
                }
            }
            return result;
        }
    }
}
