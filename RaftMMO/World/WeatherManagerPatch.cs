using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.Utilities;

namespace RaftMMO.World
{
    public static class WeatherManagerPatch
    {
        private static bool forcedWeather = false;

        [HarmonyPatch(typeof(WeatherManager), "Update")]
        public class WeatherManagerUpdatePatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(WeatherManager __instance)
            {
                if (Semih_Network.IsHost && CommonEntry.CanWePlay && BuoyManager.IsCloseEnoughToConnect())
                {
                    __instance.ForceWeather(WeatherType.Calm);
                    return false;
                }
                forcedWeather = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(WeatherManager), "StartNewWeather", typeof(int), typeof(bool))]
        public class WeatherManagerStartNewWeatherPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix()
            {
                if (Semih_Network.IsHost && CommonEntry.CanWePlay && BuoyManager.IsCloseEnoughToConnect())
                {
                    return false;
                }
                forcedWeather = false;
                return true;
            }
        }

        private static void ForceWeather(this WeatherManager weatherManager, WeatherType weatherType)
        {
            if (forcedWeather)
                return;

            Weather[] allWeathers = Traverse.Create(weatherManager).Field("weatherConnections").GetValue<Randomizer>().GetAllItems<Weather>();
            if (allWeathers == null)
                return;

            RaftMMOLogger.LogInfo("ForceWeather: " + weatherType + ", current weather: " + weatherManager.GetCurrentWeatherType());

            foreach (Weather weather in allWeathers)
            {
                RaftMMOLogger.LogVerbose("weather.name: " + weather.name);
                if (weather != null && weather.name.Contains(weatherType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    RaftMMOLogger.LogVerbose("Found: " + weather.name);

                    weatherManager.StopAllCoroutines();
                    weatherManager.StartCoroutine(weatherManager.StartNewWeather(weather, false));
                    forcedWeather = true;
                    break;
                }
            }
        }
    }
}
