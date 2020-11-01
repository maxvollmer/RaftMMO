﻿using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.World
{
    public class BuoyManager
    {
        private static float BuoyBaseHeight { get; } = 0f;
        private static float BuoyAmplitude { get; } = 0.1f;
        private static float BuoyFrequency { get; } = 0.15f;

        private static int MaxBuoySpawnTries = 8;

        private static int MaxBuoyCount { get; } = 5;
        private static int MaxBuoyDistance { get; } = 2500;
        private static int MaxSpawnBuoyDistance { get; } = 2500;
        private static int MinSpawnBuoyDistance { get; } = 500;
        public static int BuoyCollisionOverlapRadius { get; } = 500;

        public static float RaftMeetingPointDisconnectDistance { get; } = 550f;
        public static float RaftMeetingPointFavoriteConnectDistance { get; } = 450f;
        public static float RaftMeetingPointAllConnectDistance { get; } = 350f;
        public static float RaftMeetingPointPushAwayDistance { get; } = 250f;
        public static float RaftMeetingPointActiveConnectDistance { get; } = 100f;


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
                foreach (var recieverDot in RecieverDots.Values)
                {
                    Object.Destroy(recieverDot.gameObject);
                }
                Object.Destroy(buoy);
                RecieverDots.Clear();
                Location = Vector2.zero;
                buoy = null;
            }
        }

        private static List<BuoyLocation> buoyLocations = new List<BuoyLocation>();

        public static IEnumerable<SerializableData.Vector2D> BuoyLocations
        {
            get
            {
                return buoyLocations.Select(b => new SerializableData.Vector2D(b.Location));
            }
        }

        [HarmonyPatch(typeof(Reciever), "HandleUI")]
        class RecieverHandleUIPatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix(Reciever __instance)
            {
                UpdateReceiverBuoys(__instance);
            }
        }

        private static void UpdateReceiverBuoys(Reciever reciever)
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (reciever.hasSignal)
            {
                foreach (var buoyLocation in buoyLocations)
                {
                    // Create dots if not already created for this reciever
                    if (!buoyLocation.RecieverDots.ContainsKey(reciever))
                    {
                        var dotPrefab = Traverse.Create(reciever).Field("dotPrefab").GetValue<Reciever_Dot>();
                        var dotParent = Traverse.Create(reciever).Field("dotParent").GetValue<RectTransform>();
                        var recieverDot = Object.Instantiate(dotPrefab, dotParent);
                        recieverDot.transform.localScale = dotPrefab.transform.localScale;
                        recieverDot.SetTargetedByReciever(false);
                        Traverse.Create(recieverDot).Field("rect").SetValue(recieverDot.GetComponent<RectTransform>());
                        Traverse.Create(recieverDot).Field("dotImage").GetValue<Image>().color = Color.red;

                        buoyLocation.RecieverDots.Add(reciever, recieverDot);
                    }

                    var receiverDot = buoyLocation.RecieverDots.GetValueSafe(reciever);

                    Vector2 receiver2DPos = new Vector2(reciever.transform.position.x, reciever.transform.position.z);
                    Vector2 direction = buoyLocation.Location - receiver2DPos;
                    float distance = direction.magnitude;

                    if (distance < Traverse.Create(reciever).Field("radarNoticePointLength").GetValue<float>())
                    {
                        float angle = 360f - reciever.transform.eulerAngles.y + GetAngle(Vector2.up, direction);
                        Vector3 vector3 = -new Vector3(Mathf.Sin(angle * (Mathf.PI / 180f)), Mathf.Cos(angle * (Mathf.PI / 180f)), 0f);

                        float radarLength = Traverse.Create(reciever).Field("radarLength").GetValue<float>();
                        float radarUIWidth = Traverse.Create(reciever).Field("radarUIWidth").GetValue<float>();

                        float relativeDistanceOnRadar = Mathf.Clamp01(distance / radarLength);
                        float radarRadius = radarUIWidth / 2f;

                        receiverDot.gameObject.SetActiveSafe(true);
                        Traverse.Create(reciever).Field("isCurrentlyShowingRadarDot").SetValue(true);
                        receiverDot.SetLocalPosition(vector3 * (radarRadius * relativeDistanceOnRadar));
                        receiverDot.SetLengthToPoint(distance);
                        receiverDot.SetText(distance.ToString("F0"));
                    }
                    else
                    {
                        receiverDot.gameObject.SetActiveSafe(false);
                    }
                }
            }
        }

        private static float GetAngle(Vector2 dirOne, Vector2 dirTwo)
        {
            float num = Vector2.SignedAngle(dirOne, dirTwo);
            if (num < 0)
                num = 360f + num;
            return 360f - num;
        }

        public static void Update()
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (Semih_Network.IsHost)
            {
                var raft = ComponentManager<Raft>.Value;
                Vector2 raft2DPos = new Vector2(raft.transform.position.x, raft.transform.position.z);

                buoyLocations = buoyLocations.Where(IsBuoyNotTooFarAway).ToList();

                int count = 0;
                if (buoyLocations.Count < MaxBuoyCount)
                {
                    while (buoyLocations.Count < MaxBuoyCount)
                    {
                        if (GetRandomBuoyLocation(out Vector2 buoyLocation, count))
                        {
                            buoyLocations.Add(new BuoyLocation(buoyLocation));
                            count++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (buoyLocations.Count > 0)
                {
                    float smallestDistance = float.MaxValue;
                    foreach (var buoyLocation in buoyLocations)
                    {
                        float distance = (buoyLocation.Location - raft2DPos).magnitude;
                        if (distance <= smallestDistance)
                        {
                            smallestDistance = distance;
                            Globals.CurrentRaftMeetingPoint = new Vector3(buoyLocation.Location.x, 0f, buoyLocation.Location.y);
                        }
                    }
                    Globals.CurrentRaftMeetingPointDistance = smallestDistance;
                }
                else
                {
                    Globals.CurrentRaftMeetingPoint = new Vector3(9999999999999f, -9999999999999f, 9999999999999f);
                    Globals.CurrentRaftMeetingPointDistance = 9999999999999f;
                }
            }

            UpdateBuoys();
        }

        private static bool IsBuoyNotTooFarAway(BuoyLocation buoyLocation)
        {
            var raft = ComponentManager<Raft>.Value;
            Vector2 raft2DPos = new Vector2(raft.transform.position.x, raft.transform.position.z);
            float distance = (buoyLocation.Location - raft2DPos).magnitude;
            return distance <= MaxBuoyDistance;
        }

        public static void ReceiveBuyLocationsFromHost(IEnumerable<Vector2> buoyLocations)
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (Semih_Network.IsHost)
                return;

            var index = 0;
            foreach (var buoyLocation in buoyLocations)
            {
                if (BuoyManager.buoyLocations.Count < index)
                {
                    BuoyManager.buoyLocations[index].Location = buoyLocation;
                }
                else
                {
                    BuoyManager.buoyLocations.Add(new BuoyLocation(buoyLocation));
                }
                index++;
            }

            while (index < BuoyManager.buoyLocations.Count)
            {
                BuoyManager.buoyLocations[index].Destroy();
                BuoyManager.buoyLocations.RemoveAt(index);
            }
        }

        public static bool IsCloseEnoughToConnect()
        {
            return buoyLocations.Count > 0 && Globals.CurrentRaftMeetingPointDistance < RaftMeetingPointFavoriteConnectDistance;
        }

        public static bool IsCloseEnoughToConnectToAll()
        {
            return buoyLocations.Count > 0 && Globals.CurrentRaftMeetingPointDistance < RaftMeetingPointAllConnectDistance;
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

            buoyLocation = Vector2.zero;
            bool foundValidPosition = false;
            int tries = 0;
            while (!foundValidPosition && tries < MaxBuoySpawnTries)
            {
                Vector2 delta = Vector2.zero;
                while (delta.sqrMagnitude == 0f)
                    delta = new Vector2(RandomFloat(-1f, 1f), RandomFloat(-1f, 1f));

                var distance = RandomFloat(MinSpawnBuoyDistance, MaxSpawnBuoyDistance);
                delta = delta.normalized * distance;
                buoyLocation = new Vector2(raft.transform.position.x + delta.x, raft.transform.position.z + delta.y);
                foundValidPosition = IsValidBuoyLocation(buoyLocation);
                tries++;
            }

            return foundValidPosition;
        }

        private static float RandomFloat(float min, float max)
        {
            return (float)(Globals.RND.NextDouble() * (max - min) + min);
        }

        private static bool IsValidBuoyLocation(Vector2 position)
        {
            foreach (var buoyLocation in buoyLocations)
            {
                var distance = (buoyLocation.Location - position).magnitude;
                if (distance < (BuoyCollisionOverlapRadius * 2))
                    return false;
            }

            return ChunkAndBuoyCombiner.DoesBuoyFit(position);
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

        private static void UpdateBuoys()
        {
            buoyLocations.ForEach(buoyLocation =>
            {
                if (buoyLocation.buoy == null)
                {
                    buoyLocation.buoy = Object.Instantiate(Globals.AssetBundle.LoadAsset<GameObject>("buoy"));
                    buoyLocation.buoy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    buoyLocation.buoy.SetActive(true);

                    // Not sure what the best approach here is:

                    // 1st option: Buoy solid for players but not rafts: No annoyance for raft, can climb on buoy which feels immersive, but buoy can push players when raft sails through it
                    // No code needed, that's the default

                    // 2nd option: Buoy solid for rafts and players: Most realism, but annoying when you get stuck on the buoy with your raft
                    // buoy.layer = LayerMask.NameToLayer("Obstruction");

                    // 3rd option: Buoy not solid: Buoy feels out of place/unreal
                    foreach (var collider in buoyLocation.buoy.GetComponentsInChildren<Collider>())
                        Object.Destroy(collider);
                }

                if (buoyForcePosition != Vector3.zero)
                {
                    buoyLocation.buoy.transform.position = buoyForcePosition;
                    return;
                }

                float height = (BuoyAmplitude * Mathf.Sin(2.0f * Mathf.PI * BuoyFrequency * Time.time)) + BuoyBaseHeight;
                buoyLocation.buoy.transform.position = new Vector3(buoyLocation.Location.x, height, buoyLocation.Location.y);
            });
        }

        public static void DebugForceSetBuoy(Vector3 position)
        {
            buoyForcePosition = position;
        }
    }
}
