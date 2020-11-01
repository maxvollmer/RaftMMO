using HarmonyLib;
using RaftMMO.Network;
using RaftMMO.Utilities;
using RaftMMO.World;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RaftMMO.Network.Messages;
using RaftMMO.Network.SerializableData;
using RaftMMO.ModSettings;

namespace RaftMMO.Trade
{
    public class TradeManager
    {
        [HarmonyPatch(typeof(RessurectComponent), "CheckForPlayerToCarry", new Type[0], new ArgumentType[0])]
        class RessurectComponentCheckForPlayerToCarryPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(RessurectComponent __instance)
            {
                if (__instance.IsCarrying || __instance.BeingCarried)
                    return true;

                return remoteTradePlayer == null;
            }
        }

        [HarmonyPatch(typeof(Pickup), "Update")]
        class PickupUpdatePatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(Pickup __instance)
            {
                var player = Traverse.Create(__instance).Field("playerNetwork").GetValue<Network_Player>();

                if (!player.IsLocalPlayer)
                    return true;

                if (player.RessurectComponent.IsCarrying || player.RessurectComponent.BeingCarried)
                    return true;

                if (CanvasHelper.ActiveMenu != MenuType.None)
                    return true;

                var canvasHelper = Traverse.Create(__instance).Field("canvas").GetValue<CanvasHelper>();

                if (!canvasHelper.CanOpenMenu)
                    return true;

                if (remoteTradePlayer != null)
                {
                    canvasHelper.SetAimSprite(AimSprite.Default);
                    Pickup.isHoveringOverPickup = false;

                    if (MyInput.GetButtonDown("Interact"))
                    {
                        Traverse.Create(__instance).Field("displayTextManager").GetValue<DisplayTextManager>().HideDisplayTexts();
                        Traverse.Create(__instance).Field("showingText").SetValue(false);

                        TradeMenu.Open(canvasHelper, remoteTradePlayer.steamID);
                    }
                    else
                    {
                        Traverse.Create(__instance).Field("displayTextManager").GetValue<DisplayTextManager>().ShowText("Open Trade Menu", MyInput.Keybinds["Interact"].MainKey, 0, 1, true);
                        Traverse.Create(__instance).Field("showingText").SetValue(true);
                    }
                    return false;
                }

                return true;
            }
        }



        private static Network_Player remoteTradePlayer = null;
        private static PlayerTradeData lastTradeData = null;
        private static readonly Dictionary<ulong, PlayerTradeData> remotePlayersTradeData = new Dictionary<ulong, PlayerTradeData>();

        private static CompleteTradeMessage completeTradeMessage = null;
        private static CompleteTradeMessage remoteCompleteTradeMessage = null;

        public static void Update()
        {
            if (Semih_Network.IsHost && !RemoteSession.IsConnectedToPlayer)
            {
                Abort();
                return;
            }

            if (!Semih_Network.IsHost && (!ComponentManager<Semih_Network>.Value.IsConnectedToHost || !ClientSession.IsHostConnectedToPlayer))
            {
                Abort();
                return;
            }

            if (TradeMenu.IsOpen)
            {
                if (Player.LocalPlayerIsDead)
                {
                    TradeMenu.Close();
                    var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
                    localPlayer.PersonController.IsMovementFree = false;
                }
                else if (TradeMenu.TimeSinceOpened > 100 && MyInput.GetButtonDown("Cancel") && completeTradeMessage == null)
                {
                    TradeMenu.Close();
                }
            }

            if (!TradeMenu.IsOpen)
            {
                AbandonTrade();
            }

            SendTradeData();

            UpdateRemoteTradePlayer();

            if (remoteTradePlayer != null
                && remotePlayersTradeData.TryGetValue(remoteTradePlayer.steamID.m_SteamID, out PlayerTradeData remotePlayerTradeData)
                && (Globals.TEMPDEBUGConnectToLocalPlayer || SteamHelper.IsSameSteamID(remotePlayerTradeData.RemoteTradePlayerSteamID, ComponentManager<Semih_Network>.Value.LocalSteamID.m_SteamID)))
            {
                TradeMenu.UpdateRemoteTradeData(remotePlayerTradeData);
            }
            else
            {
                TradeMenu.UpdateRemoteTradeData(null);
            }

            TradeMenu.Update();
        }

        public static void Abort()
        {
            TradeMenu.Close();
            remoteTradePlayer = null;
            lastTradeData = null;
            remotePlayersTradeData.Clear();
            AbandonTrade();
        }

        public static bool HaveItemsChanged(List<Item> items1, List<Item> items2)
        {
            if (items1.Count != items2.Count)
                return true;

            for (var i = 0; i < items1.Count; i++)
            {
                if (items1[i].uniqueIndex != items2[i].uniqueIndex)
                    return true;
                if (items1[i].amount != items2[i].amount)
                    return true;
            }

            return false;
        }

        public static bool HaveItemsChanged(Item[] items1, Item[] items2)
        {
            if (items1.Length != items2.Length)
                return true;

            for (var i = 0; i < items1.Length; i++)
            {
                if (items1[i].uniqueIndex != items2[i].uniqueIndex)
                    return true;
                if (items1[i].amount != items2[i].amount)
                    return true;
            }

            return false;
        }

        private static void SendTradeData()
        {
            var offerItems = TradeMenu.OfferItems;
            var wishItems = TradeMenu.WishItems;
            var isAcceptingTrade = TradeMenu.IsAcceptingTrade;
            var remoteTradePlayerSteamID = (remoteTradePlayer!=null) ? remoteTradePlayer.steamID.m_SteamID : 0;

            if (lastTradeData == null
                || isAcceptingTrade != lastTradeData.IsAcceptingTrade
                || remoteTradePlayerSteamID != lastTradeData.RemoteTradePlayerSteamID
                || HaveItemsChanged(offerItems, lastTradeData.OfferItems)
                || HaveItemsChanged(wishItems, lastTradeData.WishItems))
            {
                MessageManager.SendTradeUpdate(offerItems, wishItems, remoteTradePlayerSteamID, isAcceptingTrade);
            }

            lastTradeData = new PlayerTradeData(offerItems, wishItems, remoteTradePlayerSteamID, isAcceptingTrade);
        }

        private static void UpdateRemoteTradePlayer()
        {
            Network_Player player = null;
            if (TradeMenu.IsOpen)
            {
                player = remoteTradePlayer;
            }
            else if (Helper.HitAtCursor(out RaycastHit hit, Player.UseDistance * 1.25f, LayerMasks.MASK_Players))
            {
                player = hit.transform.GetComponent<Network_Player>();
            }
            if (player != null && !player.IsLocalPlayer && (Globals.TEMPDEBUGConnectToLocalPlayer || RemoteRaft.IsRemotePlayer(player)))
            {
                remoteTradePlayer = player;
            }
            else
            {
                remoteTradePlayer = null;
            }
        }

        public static void HandleTradeMessage(TradeMessage message)
        {
            var steamID = message.steamID;
            if (Globals.TEMPDEBUGConnectToLocalPlayer)
            {
                steamID += 1;
            }

            remotePlayersTradeData.Remove(steamID);
            remotePlayersTradeData.Add(steamID, new PlayerTradeData(
                message.offerItems.ToList(),
                message.wishItems.ToList(),
                message.remoteTradePlayerSteamID,
                message.isAcceptingTrade
            ));
        }

        public static void InitiateTrade(List<Item> offerItems, List<Item> remoteItems, ulong tradeSteamID)
        {
            completeTradeMessage = new CompleteTradeMessage(offerItems, remoteItems, tradeSteamID);
            MessageManager.SendCompleteTradeMessage(completeTradeMessage);
            CompleteTradeIfCan();
        }

        public static void HandleCompleteTradeMessage(CompleteTradeMessage message)
        {
            if (SteamHelper.IsSameSteamID(message.remoteTradePlayerSteamID, ComponentManager<Semih_Network>.Value.LocalSteamID.m_SteamID))
            {
                remoteCompleteTradeMessage = message;
                CompleteTradeIfCan();
            }
        }

        private static void CompleteTradeIfCan()
        {
            if (completeTradeMessage != null && remoteCompleteTradeMessage != null
                && SteamHelper.IsSameSteamID(completeTradeMessage.remoteTradePlayerSteamID, remoteCompleteTradeMessage.steamID)
                && SteamHelper.IsSameSteamID(completeTradeMessage.steamID, remoteCompleteTradeMessage.remoteTradePlayerSteamID)
                && !HaveItemsChanged(completeTradeMessage.offerItems, remoteCompleteTradeMessage.remoteItems)
                && !HaveItemsChanged(completeTradeMessage.remoteItems, remoteCompleteTradeMessage.offerItems))
            {
                SettingsManager.IncrementPlayerTradeCount(completeTradeMessage.remoteTradePlayerSteamID);
                TradeMenu.CompleteTrade(completeTradeMessage.remoteItems.ToList());
                completeTradeMessage = null;
                remoteCompleteTradeMessage = null;
            }
        }

        public static void AbandonTrade()
        {
            completeTradeMessage = null;
            remoteCompleteTradeMessage = null;
        }
    }
}
