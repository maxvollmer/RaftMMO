using FMODUnity;
using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.Utilities;
using System.Reflection;
using UnityEngine;

namespace RaftMMO.World
{
    public class GroundPatch
    {
        [HarmonyPatch(typeof(PersonController), "SetCorrectGroundParent")]
        class PersonControllerSetCorrectGroundParentPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(PersonController __instance, RaycastHit groundHit)
            {
                if (!CommonEntry.CanWePlay)
                    return true;

                if (groundHit.collider == null)
                    return true;

                var player = Traverse.Create(__instance).Field("playerNetwork").GetValue<Network_Player>();
                if (RemoteRaft.IsRemotePlayer(player))
                {
                    // parent raft is set in RemoteSession.HandlePlayerPositionUpdate
                    return false;
                }

                if (RemoteRaft.IsPartOfRaft(groundHit.transform.gameObject))
                {
                    __instance.transform.SetParentSafe(RemoteRaft.Transform);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PersonController), "SetGroundMaterial")]
        class PersonControllerSetGroundMaterialPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(PersonController __instance)
            {
                if (!CommonEntry.CanWePlay)
                    return true;

                if (Physics.Raycast(__instance.transform.position, Vector3.down, out RaycastHit hitInfo, __instance.groundRayLength, LayerMasks.MASK_GroundMask, QueryTriggerInteraction.Ignore))
                {
                    if (RemoteRaft.IsPartOfRaft(hitInfo.transform.gameObject))
                    {
                        StudioEventEmitter eventEmitter_footstep = (StudioEventEmitter)typeof(PersonController).GetField("eventEmitter_footstep", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        FMOD.Studio.EventInstance eventInstance_jump = (FMOD.Studio.EventInstance)typeof(PersonController).GetField("eventInstance_jump", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        FMOD.Studio.EventInstance eventInstance_land = (FMOD.Studio.EventInstance)typeof(PersonController).GetField("eventInstance_land", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                        eventEmitter_footstep.SetParameter("material", 1.0f);
                        eventInstance_jump.setParameterValue("material", 1.0f);
                        eventInstance_land.setParameterValue("material", 1.0f);
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
