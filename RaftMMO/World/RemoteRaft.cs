using RaftMMO.Network;
using RaftMMO.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RaftMMO.RaftCopyTools;
using RaftMMO.ModSettings;
using Steamworks;

using Object = UnityEngine.Object;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.World
{
    public class RemoteRaft
    {
        public static int LastUpdateCounter { get; private set; }

        public static bool SetLastUpdateCounter(int counter)
        {
            if (counter <= 0 || counter == (LastUpdateCounter + 1))
            {
                LastUpdateCounter = counter;
                return true;
            }

            if (counter == 1 && LastUpdateCounter == -1)
            {
                RaftMMOLogger.LogWarning("RemoteRaft: First full raft update was dropped, requesting anew.");
            }
            else
            {
                if (counter == (LastUpdateCounter + 2))
                {
                    // TODO:
                    // Implement ability to request resending of last delta message if only one was dropped
                    // Currently we request a full update
                }
                RaftMMOLogger.LogWarning("RemoteRaft: Raft delta with invalid update counter: " + counter + ", expected: " + (LastUpdateCounter + 1) + ", requesting full update.");
            }

            MessageManager.RequestFullRaftUpdate();
            return false;
        }

        private static GameObject remoteRaft = null;
        private static Rigidbody remoteRaftBody = null;

        private static Dictionary<SerializableData.RaftBlockData, GameObject> blockCache = new Dictionary<SerializableData.RaftBlockData, GameObject>();

        private static Dictionary<ulong, Network_Player> remotePlayers = new Dictionary<ulong, Network_Player>();

        private static HashSet<ulong> currentValidRemotePlayerSteamIDs = new HashSet<ulong>();

        public static Vector3 RemoteRaftVelocity { get; private set; } = Vector3.zero;
        public static Vector3 RemoteRaftAngularVelocity { get; private set; } = Vector3.zero;

        public static Transform Transform
        {
            get
            {
                EnsureRaftExists();
                return remoteRaft.transform;
            }
        }

        public static Bounds CalculateBounds()
        {
            EnsureRaftExists();

            bool isFirst = true;
            Bounds bounds = new Bounds(Transform.position, Vector3.zero);
            foreach (var collider in blockCache.Values.Select(o => o.GetComponentsInChildren<Collider>()).SelectMany(r => r))
            {
                if (isFirst)
                {
                    bounds = collider.bounds;
                    isFirst = false;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return bounds;
        }

        public static void OnWorldShift(Vector3 shift)
        {
            if (RemoteSession.IsConnectedToPlayer || ClientSession.IsHostConnectedToPlayer)
            {
                EnsureRaftExists();
                remoteRaftBody.position -= shift;
            }
        }

        private static void EnsureRaftExists()
        {
            if (remoteRaft == null)
            {
                remoteRaft = new GameObject("de.maxvollmer.raftmmo.remoteraft");
                remoteRaftBody = remoteRaft.AddComponent<Rigidbody>();
                remoteRaftBody.useGravity = false;
                remoteRaftBody.isKinematic = true;
               // remoteRaftBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        public static SerializableData.RaftData GetRaftData()
        {
            return new SerializableData.RaftData(blockCache.Keys.ToArray());
        }

        public static void Update()
        {
            EnsureRaftExists();

            if (RemoteSession.IsConnectedToPlayer || ClientSession.IsHostConnectedToPlayer)
            {
                remoteRaft.SetActiveSafe(true);

                remoteRaftBody.position += RemoteRaftVelocity * Time.deltaTime;
                remoteRaftBody.rotation = Quaternion.Euler(remoteRaftBody.rotation.eulerAngles + RemoteRaftAngularVelocity * Time.deltaTime);
            }
            else
            {
                RemoveRaft();
            }
        }

        public static bool IsPartOfRaft(GameObject gameObject)
        {
            EnsureRaftExists();
            return gameObject == remoteRaft || gameObject.transform.IsChildOf(remoteRaft.transform);
        }

        public static void ReplaceRaftParts(SerializableData.RaftData raftData)
        {
            RaftMMOLogger.LogVerbose("ReplaceRaftParts: " + raftData.blockData.Length);

            foreach (var go in blockCache.Values) Object.Destroy(go);
            blockCache.Clear();

            AddRaftParts(raftData);

           RaftMMOLogger.LogVerbose("ReplaceRaftParts Done!");
        }

        public static void RemoveRaftParts(SerializableData.RaftData raftData)
        {
           RaftMMOLogger.LogVerbose("RemoveRaftParts: " + raftData.blockData.Length + ", " + blockCache.Count);

            bool couldntfindthethingerror = false;

            var blockCacheCopy = blockCache.Keys.ToArray();

            foreach (var block in raftData.blockData)
            {
                if (blockCache.Remove(block, out GameObject blockObject))
                {
                    Object.Destroy(blockObject);
                }
                else
                {
                    couldntfindthethingerror = true;
                    RaftMMOLogger.LogVerbose("RemoteRaft.RemoveRaftParts: Error: Couldn't find block: ", block);
                }
            }

            if (couldntfindthethingerror)
            {
                // Something went wrong, we are out of sync, request full raft update
                RaftMMOLogger.LogWarning("RemoteRaft.RemoveRaftParts went wrong, requesting full raft update");
                RaftMMOLogger.LogVerbose("RemoteRaft.RemoveRaftParts went wrong, requesting full raft update.\n",
                    "Blocks received for removal: ", raftData.blockData, "\n",
                    "All current blocks: ", blockCacheCopy);
                MessageManager.RequestFullRaftUpdate();
            }

           RaftMMOLogger.LogVerbose("RemoveRaftParts Done!");
        }

        public static void AddRaftParts(SerializableData.RaftData raftData)
        {
            RaftMMOLogger.LogVerbose("AddRaftParts: " + raftData.blockData.Length + ", " + blockCache.Count);

            var blockCacheCopy = blockCache.Keys.ToArray();

            RaftCopier.RestoreRaftData(raftData, remoteRaft, blockCache, out bool alreadyhadthethingerror);

            if (alreadyhadthethingerror)
            {
                // Something went wrong, we are out of sync, request full raft update
                RaftMMOLogger.LogWarning("RemoteRaft.AddRaftParts went wrong, requesting full raft update");
                RaftMMOLogger.LogVerbose("RemoteRaft.AddRaftParts went wrong, requesting full raft update.\n"
                        + "Blocks received for adding: " + raftData.blockData + "\n"
                        + "All blocks before adding: " + blockCacheCopy + "\n"
                        + "All blocks after adding: " + blockCache.Keys.ToArray());
                MessageManager.RequestFullRaftUpdate();
            }

           RaftMMOLogger.LogVerbose("AddRaftParts Done!");
        }

        private static void RemoveRaft()
        {
           RaftMMOLogger.LogVerbose("RemoveRaft");

            ClearRaft();

            remoteRaft?.SetActive(false);

            RaftMMOLogger.LogVerbose("RemoveRaft Done!");
        }

        private static void ClearRaft()
        {
           RaftMMOLogger.LogVerbose("ClearRaft");

            DetachRemoteRaftChildren();

            foreach (var go in blockCache.Values) Object.Destroy(go);
            blockCache.Clear();

            remotePlayers.ToList().ForEach(pl => Object.Destroy(pl.Value.gameObject));
            remotePlayers.Clear();
            currentValidRemotePlayerSteamIDs.Clear();

            RemoteRaftVelocity = Vector3.zero;
            RemoteRaftAngularVelocity = Vector3.zero;

            RaftMMOLogger.LogVerbose("ClearRaft Done!");
        }

        private static void DetachRemoteRaftChildren()
        {
            if (remoteRaft != null)
            {
                foreach (var person in remoteRaft.GetComponentsInChildren<PersonController>())
                {
                    person.transform.SetParentSafe(null);
                }
            }
        }

        public static void Destroy()
        {
            ClearRaft();
            Object.Destroy(remoteRaft);
            remoteRaft = null;
            remoteRaftBody = null;
            LastUpdateCounter = -1;
        }

        public static void MoveTo(Vector3 targetPos, Quaternion targetRot)
        {
            EnsureRaftExists();

            remoteRaftBody.position = targetPos;
            remoteRaftBody.rotation = targetRot;

            // TODO
            // RemoteRaftVelocity = TODO;
            // RemoteRaftAngularVelocity = TODO;
        }

        public static Network_Player GetRemotePlayer(ulong steamID, int model, Vector3 position)
        {
            if (!currentValidRemotePlayerSteamIDs.Contains(steamID))
                return null;

            if (!remotePlayers.ContainsKey(steamID))
            {
                remotePlayers.Add(steamID, FakePlayerCreator.Create(steamID, model, position));
                SettingsManager.AddMetPlayer(steamID, model);
            }

            var player = remotePlayers[steamID];
            player.playerNameTextMesh.text = player.transform.name = player.characterSettings.Name = SteamHelper.GetSteamUserName(new CSteamID(steamID), true);
            return player;
        }

        public static bool IsRemotePlayer(ulong steamID)
        {
            return remotePlayers.ContainsKey(steamID);
        }

        public static bool IsRemotePlayer(Network_Player player)
        {
            return player != null && !player.IsLocalPlayer && IsRemotePlayer(player.steamID.m_SteamID);
        }

        public static IEnumerable<Network_Player> GetRemotePlayers()
        {
            return remotePlayers.Values;
        }

        public static int GetRemotePlayerCount()
        {
            return remotePlayers.Count;
        }

        public static void RemoveRemotePlayer(ulong steamID)
        {
            currentValidRemotePlayerSteamIDs.Remove(steamID);
            if (remotePlayers.Remove(steamID, out Network_Player player))
            {
                player.gameObject.transform.SetParent(null);
                Object.Destroy(player.gameObject);
            }
        }

        public static void SetListOfPlayers(ulong[] players)
        {
            currentValidRemotePlayerSteamIDs.Clear();

            if (players != null && players.Length > 0)
            {
                currentValidRemotePlayerSteamIDs.UnionWith(players);
            }

            if (Semih_Network.IsHost && RemoteSession.IsConnectedToPlayer)
                currentValidRemotePlayerSteamIDs.Add(RemoteSession.ConnectedPlayer.m_SteamID);
            else if (!Semih_Network.IsHost && ClientSession.IsHostConnectedToPlayer)
                currentValidRemotePlayerSteamIDs.Add(ClientSession.ConnectedSteamID);

            remotePlayers.Keys.Where(steamID => !currentValidRemotePlayerSteamIDs.Contains(steamID)).ToList().ForEach(steamID =>
            {
                RemoveRemotePlayer(steamID);
            });
        }
    }
}
