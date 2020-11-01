using RaftMMO.Utilities;
using System.IO;
using UnityModManagerNet;

namespace RaftMMO.ModEntry
{
    // This file will be compiled into the RaftMMO.dll for the Unity Mod Manager by newman55
    // It will be ignored when packing the rmod archive for the RaftModLoader by traxam and TeKGamer

    public class UMMModEntry
    {
        private class ModJsonLib : IModJsonLib
        {
            // For some reason TinyJson cannot correctly serialize, and SimpleJson cannot correctly deserialize,
            // but TinyJson can correctly deserialize, and SimpleJson can correctly serialize.

            public T Deserialize<T>(string json)
            {
                return TinyJson.JSONParser.FromJson<T>(json);
            }

            public string Serialize(object o)
            {
                return SimpleJson.SimpleJson.SerializeObject(o);
            }
        }

        private class ModDataGetter : IModDataGetter
        {
            private readonly string modpath;

            public byte[] GetDataFile(string name)
            {
                return File.ReadAllBytes(modpath + "Data\\" + name);
            }

            public byte[] GetModFile(string name)
            {
                return File.ReadAllBytes(modpath + name);
            }

            public ModDataGetter(string modpath)
            {
                this.modpath = modpath;
            }
        }

        private class MMULogger : IModLogger
        {
            private readonly UnityModManager.ModEntry.ModLogger modLogger;

            public MMULogger(UnityModManager.ModEntry.ModLogger modLogger)
            {
                this.modLogger = modLogger;
            }

            public void LogError(string message)
            {
                modLogger.Error(message);
            }

            public void LogWarning(string message)
            {
                modLogger.Warning(message);
            }

            public void LogDebug(string message)
            {
                modLogger.Log(message);
            }

            public void LogInfo(string message)
            {
                modLogger.Log(message);
            }

            public void LogAlways(string message)
            {
                modLogger.Log(message);
            }
        }

        private static bool IsActive { get; set; } = false;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            RaftMMOLogger.ModLogger = new MMULogger(modEntry.Logger);

            modEntry.OnUpdate = OnUpdate;
            modEntry.OnToggle = OnToggle;

            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool active)
        {
            if (IsActive == active)
                return true;

            IsActive = active;

            if (IsActive)
            {
                CommonEntry.OnModLoad(new ModDataGetter(modEntry.Path), new ModJsonLib());
            }
            else
            {
                CommonEntry.OnModUnload();
            }

            return true;
        }

        public static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            CommonEntry.Update();
        }
    }
}
