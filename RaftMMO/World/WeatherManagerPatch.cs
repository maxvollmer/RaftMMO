using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.Utilities;

namespace RaftMMO.World
{
    public static class WeatherManagerPatch
    {
        [HarmonyPatch(typeof(WeatherManager), "Update")]
        public class WeatherManagerUpdatePatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(WeatherManager __instance)
            {
                if (Semih_Network.IsHost && CommonEntry.CanWePlay && BuoyManager.IsCloseEnoughToConnect())
                {
                    ForceWeather(__instance, UniqueWeatherType.Calm);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WeatherManager), "SetWeather", typeof(Weather), typeof(bool))]
        public class WeatherManagerSetWeatherPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(Weather weather, bool instant)
            {
                if (Semih_Network.IsHost && CommonEntry.CanWePlay && BuoyManager.IsCloseEnoughToConnect())
                {
                    return weather.so_weather.uniqueWeatherType == UniqueWeatherType.Calm && instant;
                }
                return true;
            }
        }

        private static void ForceWeather(WeatherManager weatherManager, UniqueWeatherType weatherType)
        {
            if (weatherManager.GetCurrentWeatherType() != UniqueWeatherType.Calm
                && !Traverse.Create(weatherManager).Field("changingWeather").GetValue<bool>())
            {
                RaftMMOLogger.LogInfo("ForceWeather: " + weatherType + ", current weather: " + weatherManager.GetCurrentWeatherType());
                weatherManager.SetWeather(UniqueWeatherType.Calm, true);
            }
        }
    }
}
