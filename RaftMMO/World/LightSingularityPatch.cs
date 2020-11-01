using HarmonyLib;

namespace RaftMMO.World
{
    public class LightSingularityPatch
    {
        private static LightSingularityManager lightSingularityManager = null;

        public static LightSingularityManager LightSingularityManager
        {
            get
            {
                if (lightSingularityManager == null)
                {
                    lightSingularityManager = new LightSingularityManager();
                    lightSingularityManager.lightSingularityPool = ComponentManager<LightSingularityManager>.Value.lightSingularityPool;
                    Traverse.Create(lightSingularityManager).Field("lockedPivotParent").SetValue(RemoteRaft.Transform);
                }
                return lightSingularityManager;
            }
        }

        public static void Destroy()
        {
            lightSingularityManager = null;
        }

        [HarmonyPatch(typeof(LightSingularity), "OnEnable")]
        public class LightSingularityOnEnablePatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix(LightSingularity __instance)
            {
                if (RemoteRaft.IsPartOfRaft(__instance.gameObject))
                {
                    Traverse.Create(__instance).Field("lightManager").SetValue(lightSingularityManager);
                }
            }
        }

        [HarmonyPatch(typeof(LightSingularity), "Start")]
        public class LightSingularityStartPatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix(LightSingularity __instance)
            {
                if (RemoteRaft.IsPartOfRaft(__instance.gameObject))
                {
                    Traverse.Create(__instance).Field("lightManager").SetValue(lightSingularityManager);
                }
            }
        }
    }
}
