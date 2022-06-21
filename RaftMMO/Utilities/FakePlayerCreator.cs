using HarmonyLib;
using RaftMMO.Network;
using Steamworks;
using System.Reflection;
using UnityEngine;

namespace RaftMMO.Utilities
{
    public class FakePlayerCreator
    {
        public static Network_Player Create(ulong steamID, int modelIndex, Vector3 position)
        {
            var network = ComponentManager<Raft_Network>.Value;
            Network_Player player = Object.Instantiate<Network_Player>(network.playerPrefab, position, Quaternion.identity);
            
            //if (modelIndex < 0 || modelIndex >= player.modelPrefabs.Length)
            modelIndex = 0;

            string name = SteamHelper.GetSteamUserName(new CSteamID(steamID), true);

            var characterSettings = new RGD_Settings_Character
            {
                Name = name,
                ModelIndex = modelIndex
            };

            if (steamID == network.LocalSteamID.m_SteamID)
            {
                steamID += 1;
            }

            player.characterSettings = characterSettings;
            Traverse.Create(player).Field("network").SetValue(network);
            player.steamID = new CSteamID(steamID);
            Traverse.Create(player).Field("isLocalPlayer").SetValue(false);
            player.GetType().GetTypeInfo().GetDeclaredMethod("InitializeComponents").Invoke(player, new object[] { });
            player.playerNameTextMesh.text = player.transform.name = characterSettings.Name;
            
            player.currentModel = Object.Instantiate(player.currentModel, player.Animator.transform);
            player.currentModel.transform.localPosition = player.currentModel.transform.localEulerAngles = Vector3.zero;
            player.leftHandParent.SetParent(player.currentModel.leftHandItemHolder);
            player.leftHandParent.localPosition = player.leftHandParent.localEulerAngles = Vector3.zero;
            player.leftHandParent.gameObject.SetActive(true);
            player.rightHandParent.SetParent(player.currentModel.rightHandItemHolder);
            player.rightHandParent.localPosition = player.rightHandParent.localEulerAngles = Vector3.zero;
            player.rightHandParent.gameObject.SetActive(true);

            var hatParent = Traverse.Create(player).Field("hatParent").GetValue<Transform>();
            var chestParent = Traverse.Create(player).Field("chestParent").GetValue<Transform>();
            var lLegParent = Traverse.Create(player).Field("lLegParent").GetValue<Transform>();
            var rLegParent = Traverse.Create(player).Field("rLegParent").GetValue<Transform>();

            hatParent.SetParent(player.currentModel.headBone);
            hatParent.localPosition = hatParent.localEulerAngles = Vector3.zero;
            hatParent.gameObject.SetActive(true);
            chestParent.SetParent(player.currentModel.skeletonBone);
            chestParent.localPosition = chestParent.localEulerAngles = Vector3.zero;
            chestParent.gameObject.SetActive(true);
            lLegParent.SetParent(player.Animator.anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
            lLegParent.localPosition = lLegParent.localEulerAngles = Vector3.zero;
            lLegParent.gameObject.SetActive(true);
            rLegParent.SetParent(player.Animator.anim.GetBoneTransform(HumanBodyBones.RightLowerLeg));
            rLegParent.localPosition = rLegParent.localEulerAngles = Vector3.zero;
            rLegParent.gameObject.SetActive(true);

            player.CameraTransform.SetParent(player.currentModel.cameraHolder);
            player.CameraTransform.localPosition = player.CameraTransform.localEulerAngles = Vector3.zero;
            player.Animator.anim.Rebind();

            foreach (var localBehaviour in player.localBehaviours)
                Object.Destroy(localBehaviour);

            Traverse.Create(player).Field("camera").SetValue(null);
            Traverse.Create(player).Field("cameraTransform").GetValue<Transform>().tag = "Untagged";
            player.PersonController.enabled = true;
            player.PersonController.IsMovementFree = true;

            Traverse.Create(player).Field("nameBillboard").GetValue<BillboardObject>().SetTarget(network.GetLocalPlayer().CameraTransform);

            player.SetNameTagVisibility(true);


            // TODO: TEMP WORKAROUND:
            // Maya's hair floats around relative to the local player's rotation and distance to this remote player.
            // I have debugged all transforms, game objects, and components to figure out what's going on:
            // All local positions and rotations look fine,
            // the world position and rotation of the hair object look fine,
            // all bone transforms look fine,
            // but the hair floats around and i can't figure out why :(
            // I literally put a test object at the world position of the hair,
            // and it appeared where the player model is, not where the hair is.
            // I give up.
            // So as a temporary workaround, I just disable Maya's hair here:
            var hair = player.gameObject.transform.FindChildRecursively("Female_Hair_Full");
            if (hair != null)
            {
                hair.gameObject.SetActive(false);
            }


            return player;
        }
    }
}
