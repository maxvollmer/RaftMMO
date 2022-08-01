using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.Network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RaftMMO.World
{
    public class ReceiverPatch
    {
        [HarmonyPatch(typeof(Reciever), "HandleUI")]
        class RecieverHandleUIPatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix(Reciever __instance)
            {
                UpdateReceiverDots(__instance);
            }
        }

        private static List<Dictionary<Reciever, Reciever_Dot>> buoyReceiverDots = new List<Dictionary<Reciever, Reciever_Dot>>();
        private static Dictionary<Reciever, Reciever_Dot> raftReceiverDots = new Dictionary<Reciever, Reciever_Dot>();

        private static void UpdateReceiverDots(Reciever reciever)
        {
            if (!CommonEntry.CanWePlay)
                return;

            if (reciever.hasSignal)
            {
                var buoyLocations = BuoyManager.BuoyLocations.ToList();

                while (buoyReceiverDots.Count > buoyLocations.Count)
                {
                    foreach (var buoyReceiverDot in buoyReceiverDots[buoyReceiverDots.Count].Values)
                    {
                        Object.Destroy(buoyReceiverDot.gameObject);
                    }
                    buoyReceiverDots.RemoveAt(buoyReceiverDots.Count - 1);
                }

                for (int i = 0; i < buoyLocations.Count; i++)
                {
                    if (buoyReceiverDots.Count == i)
                    {
                        buoyReceiverDots.Add(new Dictionary<Reciever, Reciever_Dot>());
                    }
                    CreateReceiverDot(reciever, buoyReceiverDots[i], Color.red);
                    UpdateReceiverDot(reciever, buoyReceiverDots[i].GetValueSafe(reciever), buoyLocations[i].Vector2);
                }

                if (RemoteSession.IsConnectedToPlayer)
                {
                    CreateReceiverDot(reciever, raftReceiverDots, Color.magenta);
                    UpdateReceiverDot(reciever, raftReceiverDots.GetValueSafe(reciever), new Vector2(RemoteRaft.Transform.position.x, RemoteRaft.Transform.position.z));
                }
                else
                {
                    foreach (var raftReceiverDot in raftReceiverDots.Values)
                    {
                        Object.Destroy(raftReceiverDot.gameObject);
                    }
                    raftReceiverDots.Clear();
                }
            }
            else
            {
                Destroy();
            }
        }

        private static void CreateReceiverDot(Reciever reciever, Dictionary<Reciever, Reciever_Dot> receiverDots, Color color)
        {
            // Create dot if not already created for this reciever
            if (!receiverDots.ContainsKey(reciever))
            {
                var dotPrefab = Traverse.Create(reciever).Field("dotPrefab").GetValue<Reciever_Dot>();
                var dotParent = Traverse.Create(reciever).Field("dotParent").GetValue<RectTransform>();
                var recieverDot = Object.Instantiate(dotPrefab, dotParent);
                recieverDot.transform.localScale = dotPrefab.transform.localScale;
                recieverDot.SetTargetedByReciever(false);
                Traverse.Create(recieverDot).Field("rect").SetValue(recieverDot.GetComponent<RectTransform>());
                Traverse.Create(recieverDot).Field("dotImage").GetValue<Image>().color = color;
                receiverDots.Add(reciever, recieverDot);
            }
        }

        private static void UpdateReceiverDot(Reciever reciever, Reciever_Dot receiverDot, Vector2 buoyLocation)
        {
            Vector2 receiver2DPos = new Vector2(reciever.transform.position.x, reciever.transform.position.z);
            Vector2 direction = buoyLocation - receiver2DPos;
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

        private static float GetAngle(Vector2 dirOne, Vector2 dirTwo)
        {
            float num = Vector2.SignedAngle(dirOne, dirTwo);
            if (num < 0)
                num = 360f + num;
            return 360f - num;
        }

        public static void Destroy()
        {
            foreach (var buoyReceiverDot in buoyReceiverDots.Select(d => d.Values).SelectMany(d => d))
            {
                Object.Destroy(buoyReceiverDot.gameObject);
            }
            foreach (var raftReceiverDot in raftReceiverDots.Values)
            {
                Object.Destroy(raftReceiverDot.gameObject);
            }
            buoyReceiverDots.Clear();
            raftReceiverDots.Clear();
        }
    }
}
