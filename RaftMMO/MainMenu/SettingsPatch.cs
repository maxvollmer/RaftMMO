using FMOD;
using HarmonyLib;

namespace RaftMMO.MainMenu
{
    public class SettingsPatch
    {
        [HarmonyPatch(typeof(Settings), "Open")]
        public class SettingsOpenPatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix()
            {
                SettingsMenuInjector.Inject();
            }
        }
    }
}
