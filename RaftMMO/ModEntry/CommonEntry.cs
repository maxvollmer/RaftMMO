using HarmonyLib;
using RaftMMO.MainMenu;
using RaftMMO.ModSettings;
using RaftMMO.Network;
using RaftMMO.RaftCopyTools;
using RaftMMO.Trade;
using RaftMMO.Utilities;
using RaftMMO.World;
using System.Reflection;
using UnityEngine;

namespace RaftMMO.ModEntry
{
    public class CommonEntry
    {
        public static IModDataGetter ModDataGetter { get; private set; } = null;
        public static IModJsonLib ModJsonLib { get; private set; } = null;

        private static Harmony HarmonyInstance { get; set; } = null;

        public static void OnModLoad(IModDataGetter modDataGetter, IModJsonLib modJsonLib)
        {
            ModDataGetter = modDataGetter;
            ModJsonLib = modJsonLib;

            SettingsSaver.InitializeSavePaths();
            SettingsSaver.Load();

            RemoteRaftScreenshotTaker.Initialize();

            WorldShiftManager.OnWorldShift += BuoyManager.OnWorldShift;
            WorldShiftManager.OnWorldShift += RemoteRaft.OnWorldShift;

            HarmonyInstance = new Harmony("de.maxvollmer.raftmmo");
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            Globals.AssetBundle = AssetBundle.LoadFromMemory(ModDataGetter.GetDataFile("raftmmo.assets"));

            ImageLoader.Initialize();

            Globals.IsDisabled = false;

            SettingsMenuInjector.Inject();

            RaftMMOLogger.LogAlways("RaftMMO has been loaded!");

            var test = new RaftMMO.Salty.Test();
        }

        public static void OnModUnload()
        {
            SettingsSaver.Save();

            Globals.IsDisabled = true;

            SettingsMenuInjector.Remove();
            SettingsMenuBuilder.Destroy();

            WorldShiftManager.OnWorldShift -= BuoyManager.OnWorldShift;
            WorldShiftManager.OnWorldShift -= RemoteRaft.OnWorldShift;

            HarmonyInstance.UnpatchAll("de.maxvollmer.raftmmo");
            HarmonyInstance = null;

            RemoteSession.Disconnect();
            TradeMenu.Destroy();
            RemoteRaft.Destroy();
            BuoyManager.Destroy();
            ClientSession.Disconnect();
            RaftDataManager.Clear();
            SteamHelper.CloseAll();
            LightSingularityPatch.Destroy();

            RemoteRaftScreenshotTaker.Destroy();

            ImageLoader.Destroy();

            Globals.AssetBundle.Unload(true);
            Globals.AssetBundle = null;

            RaftMMOLogger.LogAlways("RaftMMO has been unloaded!");

            SettingsSaver.Release();

            ModDataGetter = null;
            ModJsonLib = null;
        }

        public static bool CanWePlay
        {
            get
            {
                return !Globals.IsDisabled
                    && !Raft_Network.InMenuScene
                    && BlockCreator.GetPlacedBlocks().Count > 0
                    && (Raft_Network.IsHost || ComponentManager<Raft_Network>.Value.IsConnectedToHost);
            }
        }

        public static void Update()
        {
            if (Globals.IsDisabled)
            {
                return;
            }

            try
            {
                SettingsSaver.Update();

                if (CanWePlay)
                {
                    BuoyManager.Update();
                    RemoteSession.Update();
                    RemoteRaft.Update();
                    TradeManager.Update();
                    ClientSession.Update();
                    RaftDataManager.Update();
                    RemoteRaftScreenshotTaker.Update();
                }
                else
                {
                    RemoteSession.Disconnect();
                    TradeManager.Abort();
                    RemoteRaft.Destroy();
                    BuoyManager.Destroy();
                    ClientSession.Disconnect();
                    RaftDataManager.Clear();
                    SteamHelper.CloseAll();
                    LightSingularityPatch.Destroy();
                }
            }
            catch (System.Exception e)
            {
                RaftMMOLogger.LogError("RaftMMO has caught an exception: " + e.ToString());
            }
        }
    }
}
