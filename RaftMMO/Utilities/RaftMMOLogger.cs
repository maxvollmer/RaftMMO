using RaftMMO.ModEntry;
using RaftMMO.ModSettings;
using RaftMMO.Network.SerializableData;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RaftMMO.Utilities
{
    public class RaftMMOLogger
    {
        private static bool logToFileFailed = false;

        public static IModLogger ModLogger { get; set; } = null;

        public static void LogError(string msg)
        {
            if (SettingsManager.Settings.LogLevel >= LogLevel.ERROR)
            {
                ModLogger?.LogError("[RaftMMO] [" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "] [ERROR] " + TrimMessage(msg));
            }
            LogToFile("[ERROR] " + msg);
        }

        public static void LogWarning(string msg)
        {
            if (SettingsManager.Settings.LogLevel >= LogLevel.WARNING)
            {
                ModLogger?.LogWarning("[RaftMMO] [" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "]  [WARN] " + TrimMessage(msg));
            }
            LogToFile(" [WARN] " + msg);
        }

        public static void LogInfo(string msg)
        {
            if (SettingsManager.Settings.LogLevel >= LogLevel.INFO)
            {
                ModLogger?.LogInfo("[RaftMMO] [" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "]  [INFO] " + TrimMessage(msg));
            }
            LogToFile(" [INFO] " + msg);
        }

        public static void LogDebug(string msg)
        {
            if (SettingsManager.Settings.LogLevel >= LogLevel.DEBUG)
            {
                ModLogger?.LogDebug("[RaftMMO] [" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "] [DEBUG] " + TrimMessage(msg));
            }
            LogToFile("[DEBUG] " + msg);
        }

        public static void LogAlways(string msg)
        {
            ModLogger?.LogAlways("[RaftMMO] [" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "]  [INFO] " + TrimMessage(msg));
            LogToFile(" [INFO] " + msg);
        }

        public static void LogVerbose(string msg, params object[] stuffs)
        {
            if (!SettingsManager.Settings.LogVerbose)
                return;

            foreach (var stuff in stuffs)
            {
                if (stuff is RaftBlockData[] raftBlockData)
                {
                    msg += GameObjectDebugger.DebugPrint(raftBlockData);
                }
                else
                {
                    msg += stuff.ToString();
                }
            }

            LogToFile(" [SPAM] " + msg);
        }

        private static string TrimMessage(string msg)
        {
            bool needsEllipsis = false;

            if (msg.Contains("\n"))
            {
                msg = msg.Split('\n')[0].Trim();
                needsEllipsis = true;
            }

            if (msg.Length > 100)
            {
                msg = msg.Substring(0, 100).Trim();
                needsEllipsis = true;
            }

            if (needsEllipsis)
            {
                msg = msg + "...";
            }

            return msg;
        }

        private static void LogToFile(string msg)
        {
            try
            {
                File.AppendAllText(SettingsSaver.LogFile, "[" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss.fff") + "] " + msg + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                if (!logToFileFailed)
                {
                    logToFileFailed = true;
                    Debug.LogError("Couldn't log to file: " + e);
                }
            }
        }
    }
}
