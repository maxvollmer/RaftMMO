
using RaftMMO.ModSettings;
using RaftMMO.Network;
using RaftMMO.Utilities;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace RaftMMO
{
    public class RaftMMOCoroutines : MonoBehaviour
    {
        private static Coroutine uploadLocalRaftCoroutine = null;
        private static Coroutine uploadPosUpdateCoroutine = null;

        private static int raftUpdateCounter = 0;


        private static RaftMMOCoroutines _instance = null;
        private static void CreateInstance()
        {
            if (_instance == null)
            {
                var raft = ComponentManager<Raft>.Value;
                if (raft != null)
                {
                    _instance = raft.gameObject.AddComponent<RaftMMOCoroutines>();
                }
            }
        }

        internal static void Destroy()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }

        internal static void StartUploadLocalRaftCoroutine()
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("StartUploadLocalRaftCoroutine");
            }

            CreateInstance();
            if (_instance != null)
            {
                _instance.IntrnlStartUploadLocalRaftCoroutine();
            }
        }

        internal static void StartUploadPosUpdateCoroutine()
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("StartUploadPosUpdateCoroutine");
            }

            CreateInstance();
            if (_instance != null)
            {
                _instance.IntrnlStartUploadPosUpdateCoroutine();
            }
        }

        internal static void StopUploadLocalRaftCoroutine()
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("StopUploadLocalRaftCoroutine");
            }

            if (_instance != null)
            {
                _instance.IntrnlStopUploadLocalRaftCoroutine();
            }
        }

        internal static void StopUploadPosUpdateCoroutine()
        {
            if (SettingsManager.Settings.LogVerbose)
            {
                RaftMMOLogger.LogVerbose("StopUploadPosUpdateCoroutine");
            }

            if (_instance != null)
            {
                _instance.IntrnlStopUploadPosUpdateCoroutine();
            }
        }



        private void IntrnlStartUploadLocalRaftCoroutine()
        {
            if (uploadLocalRaftCoroutine == null)
            {
                uploadLocalRaftCoroutine = StartCoroutine(UploadLocalRaftCoroutine());
            }
        }

        private void IntrnlStartUploadPosUpdateCoroutine()
        {
            if (uploadPosUpdateCoroutine == null)
            {
                uploadPosUpdateCoroutine = StartCoroutine(UploadPosUpdateCoroutine());
            }
        }

        private void IntrnlStopUploadLocalRaftCoroutine()
        {
            if (uploadLocalRaftCoroutine != null)
            {
                StopCoroutine(uploadLocalRaftCoroutine);
                uploadLocalRaftCoroutine = null;
            }
            raftUpdateCounter = 0;
        }

        private void IntrnlStopUploadPosUpdateCoroutine()
        {
            if (uploadPosUpdateCoroutine != null)
            {
                StopCoroutine(uploadPosUpdateCoroutine);
                uploadPosUpdateCoroutine = null;
            }
        }

        private IEnumerator UploadLocalRaftCoroutine()
        {
            while (true)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose("UploadLocalRaftCoroutine");
                }

                var uploadRoutine = MessageManager.UploadLocalRaft(RemoteSession.ConnectedPlayer, raftUpdateCounter);
                while (uploadRoutine.MoveNext())
                {
                    raftUpdateCounter = uploadRoutine.Current;
                    yield return null;
                }

                MessageManager.UploadListOfPlayers(RemoteSession.ConnectedPlayer, ClientSession.GetSessionPlayers().Select(p => p.steamID.m_SteamID).ToArray());

                yield return new WaitForSeconds(Globals.RaftUpdateFrequency / 1000f);
            }
        }

        private IEnumerator UploadPosUpdateCoroutine()
        {
            while (true)
            {
                if (SettingsManager.Settings.LogVerbose)
                {
                    RaftMMOLogger.LogVerbose("UploadPosUpdateCoroutine");
                }

                MessageManager.UploadLocalPosUpdates(RemoteSession.ConnectedPlayer);
                yield return new WaitForSeconds(Globals.PositionUpdateFrequency / 1000f);
            }
        }
    }
}
