using RaftMMO.ModEntry;
using RaftMMO.ModSettings;
using RaftMMO.Network;
using RaftMMO.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Object = UnityEngine.Object;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.World
{
    public class BuoyManager
    {
        private static float BuoyBaseHeight { get; } = 0f;
        private static float BuoyAmplitude { get; } = 0.1f;
        private static float BuoyFrequency { get; } = 0.15f;


        private static float BUOY_SINK_SPEED { get; } = 0.5f;       // sinking speed of a sinking buoy in meters/second
        private static float BUOY_SINK_DISTANCE { get; } = -50;     // meters under sea level when a sinking buoy gets destroyed

        private static readonly int NUM_BUOY_SPAWN_TRIES = 64;
        private static readonly float BUOY_SPAWN_TRY_DISTANCE = 50f;

        private static float BuoySpawnDistance { get; } = 250f;


        // distances to buoy (meeting point)
        public static float RaftMeetingPointDisconnectDistance { get; } = 350f;
        public static float RaftMeetingPointConnectDistance { get; } = 300f;
        public static float RaftMeetingPointPushAwayDistance { get; } = 300f;
        public static float RaftMeetingPointActiveConnectDistance { get; } = 50f;


        // distances to remote raft
        public static float RemoteRaftVisibleDistance { get; } = 500f;


        private class BuoyLocation
        {
            public Vector2 Location { get; set; } = Vector2.zero;
            public GameObject buoy = null;
            public Dictionary<Reciever, Reciever_Dot> RecieverDots { get; } = new Dictionary<Reciever, Reciever_Dot>();

            public BuoyLocation(Vector2 location)
            {
                Location = location;
            }

            public void Destroy()
            {
                foreach (var recieverDot in RecieverDots.Values.Where(d => d?.gameObject != null))
                {
                    Object.Destroy(recieverDot.gameObject);
                }
                if (buoy != null)
                {
                    Object.Destroy(buoy);
                }
                RecieverDots.Clear();
                Location = Vector2.zero;
                buoy = null;
            }
        }

        private static List<BuoyLocation> buoyLocations = new List<BuoyLocation>();
        private static List<BuoyLocation> sinkingBuoyLocations = new List<BuoyLocation>();
        private static bool wasConnected = false;

        public static IEnumerable<SerializableData.Vector2D> VisibleBuoyLocations
        {
            get
            {
                return buoyLocations.Where(b => b.buoy?.activeSelf ?? false).Select(b => new SerializableData.Vector2D(b.Location));
            }
        }

        public static IEnumerable<SerializableData.Vector2D> SinkingBuoyLocations
        {
            get
            {
                return sinkingBuoyLocations.Select(b => new SerializableData.Vector2D(b.Location));
            }
        }

        public static void Update()
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (Raft_Network.IsHost)
            {
                if (RemoteSession.IsConnectedToPlayer)
                {
                    wasConnected = true;
                }
                else
                {
                    if (wasConnected)
                    {
                        sinkingBuoyLocations.AddRange(buoyLocations);
                        buoyLocations.Clear();
                        wasConnected = false;
                    }

                    if (GetRandomBuoyLocation(out Vector2 buoyLocation, 0))
                    {
                        if (buoyLocations.Count == 0)
                        {
                            buoyLocations.Add(new BuoyLocation(buoyLocation));
                        }
                        else
                        {
                            buoyLocations[0].Location = buoyLocation;
                        }

                        var raft = ComponentManager<Raft>.Value;
                        Vector2 raft2DPos = new Vector2(raft.transform.position.x, raft.transform.position.z);

                        Globals.CurrentRaftMeetingPointDistance = (buoyLocation - raft2DPos).magnitude;
                        Globals.CurrentRaftMeetingPoint = new Vector3(buoyLocation.x, 0f, buoyLocation.y);

                        if (SettingsManager.Settings.LogVerbose)
                        {
                            if (SettingsManager.Settings.LogVerbose)
                            {
                                RaftMMOLogger.LogVerbose($"Globals.CurrentRaftMeetingPointDistance: {Globals.CurrentRaftMeetingPointDistance}");
                            }
                        }
                    }
                    else
                    {
                        Globals.CurrentRaftMeetingPoint = new Vector3(9999999999999f, -9999999999999f, 9999999999999f);
                        Globals.CurrentRaftMeetingPointDistance = 9999999999999f;
                    }
                }
            }

            UpdateBuoys();
        }

        private static void UpdateBuoyLocationsList(List<BuoyLocation> oldBuoyLocations, IEnumerable<Vector2> newBuoyLocations)
        {
            var index = 0;
            foreach (var newBuoyLocation in newBuoyLocations)
            {
                if (oldBuoyLocations.Count < index)
                {
                    oldBuoyLocations[index].Location = newBuoyLocation;
                }
                else
                {
                    oldBuoyLocations.Add(new BuoyLocation(newBuoyLocation));
                }
                index++;
            }

            while (index < oldBuoyLocations.Count)
            {
                oldBuoyLocations[index].Destroy();
                oldBuoyLocations.RemoveAt(index);
            }
        }

        public static void ReceiveBuyLocationsFromHost(IEnumerable<Vector2> newBuoyLocations, IEnumerable<Vector2> newSinkingBuoyLocations)
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (Raft_Network.IsHost)
                return;

            UpdateBuoyLocationsList(buoyLocations, newBuoyLocations);
            UpdateBuoyLocationsList(sinkingBuoyLocations, newSinkingBuoyLocations);
        }

        public static bool IsCloseEnoughToConnect()
        {
            var bla = buoyLocations.Count > 0 && Globals.CurrentRaftMeetingPointDistance < RaftMeetingPointConnectDistance;

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose($"IsCloseEnoughToConnect: {bla}, CurrentRaftMeetingPointDistance: {Globals.CurrentRaftMeetingPointDistance}, RaftMeetingPointConnectDistance: {RaftMeetingPointConnectDistance}, buoyLocations.Count: {buoyLocations.Count}");
            }

            return bla;
        }

        public static bool IsCloseEnoughToBeVisible()
        {
            if (buoyLocations.Count == 0)
                return false;

            var raft = ComponentManager<Raft>.Value;
            var raft2DPos = new Vector2(raft.transform.position.x, raft.transform.position.z);
            var remoteRaft2DPos = new Vector2(RemoteRaft.Transform.position.x, RemoteRaft.Transform.position.z);
            float remoteRaftDistance = (remoteRaft2DPos - raft2DPos).magnitude;

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose($"IsCloseEnoughToBeVisible raft2DPos: {raft2DPos.x}, {raft2DPos.y}, remoteRaft2DPos: {remoteRaft2DPos.x}, {remoteRaft2DPos.y}, remoteRaftDistance: {remoteRaftDistance}");
            }

            if ((raft2DPos.magnitude == 0 && !Globals.TEMPDEBUGStaticBuoyPosition)
                || remoteRaft2DPos.magnitude == 0
                || remoteRaftDistance == 0)
            {
                RaftMMOLogger.LogDebug($"IsCloseEnoughToBeVisible got invalid positions, should be fixed next frame");
                return false;
            }

            return remoteRaftDistance < RemoteRaftVisibleDistance;
        }

        public static bool IsFarEnoughToDisconnect()
        {
            return buoyLocations.Count == 0 || Globals.CurrentRaftMeetingPointDistance > RaftMeetingPointDisconnectDistance;
        }

        public static bool IsTooCloseToActivelyConnect()
        {
            if (buoyLocations.Count == 0)
                return false;

            if (Globals.CurrentRaftMeetingPointDistance > RaftMeetingPointActiveConnectDistance)
                return false;

            var bounds = LocalRaft.CalculateBounds();
            bounds.center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            bounds.extents = new Vector3(bounds.extents.x, 10f, bounds.extents.z);
            bounds.Expand(10f);
            return bounds.Contains(Globals.CurrentRaftMeetingPoint);
        }


        private static bool GetRandomBuoyLocation(out Vector2 buoyLocation, int count)
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("GetRandomBuoyLocation");
            }

            var raft = ComponentManager<Raft>.Value;

            if (Globals.TEMPDEBUGStaticBuoyPosition)
            {
                var bounds = LocalRaft.CalculateBounds();
                bounds.Expand(10f);
                switch (count)
                {
                    case 0:
                        buoyLocation = new Vector3(bounds.min.x, bounds.min.z);
                        break;
                    case 1:
                        buoyLocation = new Vector3(bounds.max.x, bounds.max.z);
                        break;
                    case 2:
                        buoyLocation = new Vector3(bounds.min.x, bounds.max.z);
                        break;
                    case 3:
                    default:
                        buoyLocation = new Vector3(bounds.max.x, bounds.min.z);
                        break;
                }
                return true;
            }

            Vector2 velocity = new Vector2(raft.Velocity.x, raft.Velocity.z);

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose($"GetRandomBuoyLocation velocity: {velocity.x}, {velocity.y}");
            }

            if (velocity.sqrMagnitude <= Vector2.kEpsilonNormalSqrt)
            {
                buoyLocation = Vector2.zero;
                return false;
            }

            Vector2 delta = velocity.normalized;
            if (delta.sqrMagnitude <= Vector2.kEpsilonNormalSqrt)
            {
                buoyLocation = Vector2.zero;
                return false;
            }

            delta = delta.normalized * BuoySpawnDistance;

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose($"GetRandomBuoyLocation delta: {delta.x}, {delta.y}");
            }

            var baseBuoyLocation = new Vector2(raft.transform.position.x + delta.x, raft.transform.position.z + delta.y);
            buoyLocation = baseBuoyLocation;
            bool isValidLocation = false;
            int numtries = 0;
            while (!isValidLocation && numtries < NUM_BUOY_SPAWN_TRIES)
            {
                numtries++;
                isValidLocation = ChunkAndBuoyCombiner.DoesBuoyFit(buoyLocation, out int numIslandsInWay);
                if (!isValidLocation)
                {
                    var offsetDir = new Vector2(RandomFloat(-1f, 1f), RandomFloat(-1f, 1f));
                    offsetDir.Normalize();
                    float offsetDistance = RandomFloat(0f, BUOY_SPAWN_TRY_DISTANCE);
                    buoyLocation = baseBuoyLocation + (offsetDir * offsetDistance);
                    Globals.CurrentRaftMeetingPointNumIslandsInWay = Math.Max(numIslandsInWay, Globals.CurrentRaftMeetingPointNumIslandsInWay);
                }
            }

            if (!isValidLocation)
            {
                if (buoyLocations.Count > 0
                    && buoyLocations[0].Location.x == Globals.CurrentRaftMeetingPoint.x
                    && buoyLocations[0].Location.y == Globals.CurrentRaftMeetingPoint.z
                    && IsCloseEnoughToConnect())
                {
                    if (ChunkAndBuoyCombiner.DoesBuoyFit(buoyLocations[0].Location, out int numIslandsInWay))
                    {
                        buoyLocation = buoyLocations[0].Location;
                        isValidLocation = true;
                    }
                    else
                    {
                        Globals.CurrentRaftMeetingPointNumIslandsInWay = Math.Max(numIslandsInWay, Globals.CurrentRaftMeetingPointNumIslandsInWay);
                    }
                }
            }

            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose($"GetRandomBuoyLocation done: {buoyLocation.x}, {buoyLocation.y} ({isValidLocation})");
            }

            if (isValidLocation)
            {
                Globals.CurrentRaftMeetingPointNumIslandsInWay = 0;
            }

            return isValidLocation;
        }

        private static float RandomFloat(float min, float max)
        {
            return (float)(Globals.RND.NextDouble() * (max - min) + min);
        }

        public static void Destroy()
        {
            buoyLocations.ForEach(buoyLocation => buoyLocation.Destroy());
            buoyLocations.Clear();

            Globals.CurrentRaftMeetingPoint = new Vector3(9999999999999f, -9999999999999f, 9999999999999f);
            Globals.CurrentRaftMeetingPointDistance = 9999999999999f;
            Globals.CurrentPushAwayOffset= Vector3.zero;
        }

        public static void OnWorldShift(Vector3 shift)
        {
            if (!CommonEntry.CanWePlay)
                return;

            Globals.CurrentRaftMeetingPoint -= shift;
            buoyLocations.ForEach(buoyLocation => buoyLocation.Location -= new Vector2(shift.x, shift.z));
        }

        private static Vector3 buoyForcePosition = Vector3.zero;

        private static GameObject CreateBuoyObject()
        {
            GameObject buoy = Object.Instantiate(Globals.AssetBundle.LoadAsset<GameObject>("buoy"));
            buoy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            buoy.SetActive(false);

            // Not sure what the best approach here is:

            // 1st option: Buoy solid for players but not rafts: No annoyance for raft, can climb on buoy which feels immersive, but buoy can push players when raft sails through it
            // No code needed, that's the default

            // 2nd option: Buoy solid for rafts and players: Most realism, but annoying when you get stuck on the buoy with your raft
            // buoy.layer = LayerMask.NameToLayer("Obstruction");

            // 3rd option: Buoy not solid: Buoy feels out of place/unreal, but is also the least disruptive/annoying
            // going for this one for now
            foreach (var collider in buoy.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);

            return buoy;
        }

        private static void UpdateBuoys()
        {
            buoyLocations.ForEach(buoyLocation =>
            {
                if (buoyLocation.buoy == null)
                {
                    buoyLocation.buoy = CreateBuoyObject();
                }

                if ((Raft_Network.IsHost && RemoteSession.IsConnectedToPlayer)
                    || (!Raft_Network.IsHost && ClientSession.IsHostConnectedToPlayer))
                {
                    buoyLocation.buoy.SetActive(true);
                }
                else
                {
                    buoyLocation.buoy.SetActive(false);
                }

                var smoke = buoyLocation.buoy.GetComponentInChildren<ParticleSystem>();
                if (smoke != null)
                {
                    var emission = smoke.emission;
                    emission.enabled = SettingsManager.Settings.EnableBuoySmoke;
                }

                if (buoyForcePosition != Vector3.zero)
                {
                    buoyLocation.buoy.transform.position = buoyForcePosition;
                    return;
                }

                float height = (BuoyAmplitude * Mathf.Sin(2.0f * Mathf.PI * BuoyFrequency * Time.time)) + BuoyBaseHeight;
                buoyLocation.buoy.transform.position = new Vector3(buoyLocation.Location.x, height, buoyLocation.Location.y);
            });

            sinkingBuoyLocations.ForEach(sinkingBuoyLocation =>
            {
                if (sinkingBuoyLocation.buoy == null)
                {
                    sinkingBuoyLocation.buoy = CreateBuoyObject();
                    sinkingBuoyLocation.buoy.SetActive(true);
                    float spawnHeight = (BuoyAmplitude * Mathf.Sin(2.0f * Mathf.PI * BuoyFrequency * Time.time)) + BuoyBaseHeight;
                    sinkingBuoyLocation.buoy.transform.position = new Vector3(sinkingBuoyLocation.Location.x, spawnHeight, sinkingBuoyLocation.Location.y);
                }

                var smoke = sinkingBuoyLocation.buoy.GetComponentInChildren<ParticleSystem>();
                if (smoke != null)
                {
                    var emission = smoke.emission;
                    emission.enabled = false;
                }

                float height = sinkingBuoyLocation.buoy.transform.position.y - Time.deltaTime * BUOY_SINK_SPEED;
                sinkingBuoyLocation.buoy.transform.position = new Vector3(sinkingBuoyLocation.Location.x, height, sinkingBuoyLocation.Location.y);

                if (height < BUOY_SINK_DISTANCE)
                {
                    sinkingBuoyLocation.buoy.SetActive(false);
                    sinkingBuoyLocation.Destroy();
                }
            });

            sinkingBuoyLocations = sinkingBuoyLocations.Where(b => b.buoy != null).ToList();
        }

        private static Material CreateSmokeMaterial()
        {
            Material material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            foreach (Texture pText in Resources.FindObjectsOfTypeAll<Texture>())
            {
                if (pText.name == "Default-Particle")
                {
                    material.mainTexture = pText;
                    break;
                }
            }
            return material;
        }

        public static void DebugForceSetBuoy(Vector3 position)
        {
            buoyForcePosition = position;
        }
    }
}
