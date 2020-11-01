using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using RaftMMO.ModEntry;
using RaftMMO.Utilities;

namespace RaftMMO.ModSettings
{
    public class SettingsSaver
    {
        public static string SavePath
        {
            get
            {
                return SaveAndLoad.AppPath + "de.maxvollmer.raftmmo\\";
            }
        }

        public static string ScreenshotsPath
        {
            get
            {
                return SavePath + "remoterafts\\";
            }
        }

        public static string LogPath
        {
            get
            {
                return SavePath + "logs\\";
            }
        }

        public static string SettingsFile
        {
            get
            {
                return SavePath + "settings.json";
            }
        }

        private static string logfile = null;

        public static string LogFile
        {
            get
            {
                if (logfile == null)
                {
                    logfile = "raftmmolog_"+ DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffffff") + ".txt";
                }
                return LogPath + logfile;
            }
        }

        public static bool IsDirty { get; set; } = false;
        private static Stopwatch LastSaveTimeStopwatch { get; } = new Stopwatch();

        public static void InitializeSavePaths()
        {
            logfile = null;
            try
            {
                if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
                if (!Directory.Exists(ScreenshotsPath)) Directory.CreateDirectory(ScreenshotsPath);
                if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            }
            catch (Exception e)
            {
                RaftMMOLogger.LogError("RaftMMO: Couldn't make sure save folders for mod exist: " + e);
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile, Encoding.UTF8);
                    SettingsManager.Settings.Deserialize(CommonEntry.ModJsonLib.Deserialize<ModSettings.SerializableModSettings>(json));
                }
            }
            catch (Exception e)
            {
                RaftMMOLogger.LogError("RaftMMO: Couldn't load settings: " + e);
            }
            IsDirty = false;
        }

        public static void Save()
        {
            try
            {
                string json = CommonEntry.ModJsonLib.Serialize(SettingsManager.Settings.Serialize());
                File.WriteAllText(SettingsFile, json, Encoding.UTF8);
            }
            catch(Exception e)
            {
                RaftMMOLogger.LogError("RaftMMO: Couldn't save settings: " + e);
            }
            IsDirty = false;
            LastSaveTimeStopwatch.Restart();
        }

        public static void Release()
        {
            logfile = null;
        }

        public static void Update()
        {
            if (IsDirty && (!LastSaveTimeStopwatch.IsRunning || LastSaveTimeStopwatch.ElapsedMilliseconds > 1000))
            {
                Save();
                IsDirty = false;
            }
        }
    }
}
