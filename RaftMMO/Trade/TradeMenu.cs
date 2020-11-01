using HarmonyLib;
using RaftMMO.ModSettings;
using RaftMMO.Network;
using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static UnityEngine.UI.Button;
using SerializableData = RaftMMO.Network.SerializableData;

namespace RaftMMO.Trade
{
    public class TradeMenu
    {
        private readonly static Stopwatch timeSinceOpened = new Stopwatch();
        private readonly static Stopwatch tradeStopWatch = new Stopwatch();

        private static GameObject tradeInventoryGameObject = null;
        private static TradeInventory tradeInventory = null;
        private static GameObject tradingWithText = null;
        private static GameObject waitingText = null;
        private static GameObject addAsFriendButton = null;
        private static GameObject chatButton = null;
        private static GameObject blockUserButton = null;
        private static GameObject blockRaftButton = null;
        private static GameObject confirmButton = null;
        private static GameObject nevermindButton = null;
        private static GameObject acceptButton = null;
        private static GameObject modifyButton = null;
        private static GameObject wishList = null;
        private static GameObject remoteWishList = null;
        private static GameObject wishListSelector = null;
        private static Slot wishListSelectorSlot = null;
        private static List<Slot> localSlots = new List<Slot>();
        private static List<Slot> remoteSlots = new List<Slot>();
        private static List<Slot> wishListSlots = new List<Slot>();
        private static List<Slot> remoteWishListSlots = new List<Slot>();
        private static List<Slot> wishListSelectorSlots = new List<Slot>();
        private static List<GameObject> acceptCheckboxes = new List<GameObject>();
        private static List<GameObject> remoteCheckboxes = new List<GameObject>();
        private static ulong tradeSteamID = 0;
        private static Stopwatch updateSocialButtonsStopwatch = new Stopwatch();
        private static bool isBlockingRaft = false;
        private static bool isBlockingUser = false;
        private static bool buttonPositionsAreHost = true;

        public static List<SerializableData.Item> RemoteItems
        {
            get
            {
                return remoteSlots.Where(s => !s.IsEmpty).Select(s => new SerializableData.Item(s.itemInstance.UniqueIndex, s.itemInstance.Amount, s.itemInstance.Uses)).ToList();
            }
        }

        public static List<SerializableData.Item> WishItems {
            get {
                return wishListSlots.Where(s => !s.IsEmpty).Select(s => new SerializableData.Item(s.itemInstance.baseItem.UniqueIndex, s.itemInstance.Amount, s.itemInstance.Uses)).ToList();
            }
        }

        public static List<SerializableData.Item> OfferItems
        {
            get
            {
                return localSlots.Where(s => !s.IsEmpty).Select(s => new SerializableData.Item(s.itemInstance.baseItem.UniqueIndex, s.itemInstance.Amount, s.itemInstance.Uses)).ToList();
            }
        }

        [HarmonyPatch(typeof(Inventory), "HoverEnter", typeof(Slot), typeof(PointerEventData))]
        class InventoryHoverEnterPatch
        {
            [HarmonyPostfix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static void Postfix(Inventory __instance, Slot slot)
            {
                var toSlot = Traverse.Create(typeof(Inventory)).Field("toSlot").GetValue<Slot>();
                if (remoteSlots.Contains(toSlot)
                    || wishListSlots.Contains(toSlot)
                    || remoteWishListSlots.Contains(toSlot)
                    || wishListSelectorSlots.Contains(toSlot)
                    || (__instance is TradeInventory && __instance.allSlots.Count == 0 && localSlots.Contains(toSlot)))
                {
                    Traverse.Create(typeof(Inventory)).Field("toSlot").SetValue(null);
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), "MoveItem", typeof(Slot), typeof(PointerEventData))]
        class InventoryMoveItemPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(Inventory __instance, Slot slot)
            {
                if (remoteSlots.Contains(slot)
                    || wishListSlots.Contains(slot)
                    || remoteWishListSlots.Contains(slot)
                    || wishListSelectorSlots.Contains(slot)
                    || (__instance is TradeInventory && __instance.allSlots.Count == 0))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Inventory), "ShiftMoveItem", typeof(Slot), typeof(PointerEventData))]
        class InventoryShiftMoveItemPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(Inventory __instance, Slot slot)
            {
                if (remoteSlots.Contains(slot)
                    || wishListSlots.Contains(slot)
                    || remoteWishListSlots.Contains(slot)
                    || wishListSelectorSlots.Contains(slot)
                    || (__instance is TradeInventory && __instance.allSlots.Count == 0))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Slot), "OnPointerDown", typeof(PointerEventData))]
        class SlotOnPointerDownPatch
        {
            [HarmonyPrefix]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Harmony Patch")]
            static bool Prefix(Slot __instance, PointerEventData eventData)
            {
                if (wishListSlots.Contains(__instance))
                {
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        OpenWishListSelector(__instance);
                    }
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        __instance.SetItem(null);
                    }
                }
                else if (wishListSelectorSlots.Contains(__instance))
                {
                    FinishWishListSelector(__instance.itemInstance);
                }
                return true;
            }
        }

        private class WishListInventory : TradeInventory
        {
            protected override bool IsWishList { get; } = true;
        }

        private class TradeInventory : Inventory
        {
            protected virtual bool IsWishList { get; } = false;

            protected override void Awake()
            {
                var playerInventory = ComponentManager<PlayerInventory>.Value;
                this.slotPrefab = playerInventory.slotPrefab;

                this.invRectTransform = GetComponent<RectTransform>();
                this.hoverTransform = (RectTransform)Instantiate(playerInventory.hoverTransform.gameObject).transform;
                this.darkenedTransform = (RectTransform)Instantiate(playerInventory.darkenedTransform.gameObject).transform;
                this.draggingImage = Instantiate(playerInventory.draggingImage.gameObject).GetComponent<Image>();
                this.draggingText = this.draggingImage.GetComponentInChildren<Text>();
                this.gridLayoutGroup = playerInventory.gridLayoutGroup;

                this.hoverTransform.SetParent(gameObject.transform, false);
                this.darkenedTransform.SetParent(gameObject.transform, false);
                this.draggingImage.transform.SetParent(gameObject.transform, false);

                Vector2 cellSize = playerInventory.gridLayoutGroup.cellSize;

                foreach (var slot in GetComponentsInChildren<Slot>())
                {
                    slot.InitializeEventListeners(this);
                    cellSize = slot.rectTransform.sizeDelta;
                }
                EnableSlots();

                this.hoverTransform.sizeDelta = cellSize;
                this.darkenedTransform.sizeDelta = cellSize;
                this.draggingImage.rectTransform.sizeDelta = playerInventory.gridLayoutGroup.cellSize * 0.75f;
            }

            protected override void Start()
            {
                base.Start();
                this.gameObject.SetActiveSafe(true);
            }

            protected override bool ContainsSlot(Slot slot)
            {
                return !remoteSlots.Contains(slot) && !remoteWishListSlots.Contains(slot) && base.ContainsSlot(slot);
            }

            public void DisableSlots()
            {
                base.allSlots.Clear();
            }

            public void EnableSlots()
            {
                if (IsWishList)
                    base.allSlots.AddRange(wishListSlots);
                else
                    base.allSlots.AddRange(localSlots);
            }

            protected override void Update()
            {
                base.Update();
                hoverTransform.gameObject.SetActiveSafe(hoverSlot != null && ContainsSlot(hoverSlot));
                if (hoverTransform.gameObject.activeInHierarchy)
                    hoverTransform.position = hoverSlot.rectTransform.position;
            }
        }

        public static void Update()
        {
            if (IsOpen)
            {
                if (!updateSocialButtonsStopwatch.IsRunning || updateSocialButtonsStopwatch.ElapsedMilliseconds > 1000)
                {
                    UpdateSocialButtons();
                    updateSocialButtonsStopwatch.Restart();
                }
            }

            if (tradeStopWatch.IsRunning)
            {
                if (tradeStopWatch.ElapsedMilliseconds > 2000)
                {
                    tradeStopWatch.Stop();
                    tradeStopWatch.Reset();
                    OnModify();
                    TradeManager.AbandonTrade();
                }

                return;
            }

            if (!HasRemotePlayerTradeData)
                return;

            if (RemotePlayerTradeData != null)
            {
                if (TradeManager.HaveItemsChanged(RemoteItems, RemotePlayerTradeData.OfferItems))
                {
                    OnModify();
                }

                FillSlotsWithItems(remoteSlots, RemotePlayerTradeData.OfferItems);
                FillSlotsWithItems(remoteWishListSlots, RemotePlayerTradeData.WishItems);

                remoteCheckboxes.ForEach(go => go.SetActiveSafe(RemotePlayerTradeData.IsAcceptingTrade));

                if (RemotePlayerTradeData.IsAcceptingTrade && IsAcceptingTrade)
                {
                    TradeManager.InitiateTrade(OfferItems, RemoteItems, tradeSteamID);
                    tradeStopWatch.Restart();
                }
            }
            else
            {
                remoteSlots.ForEach(s => s.SetItem(null));
                remoteWishListSlots.ForEach(s => s.SetItem(null));
                remoteCheckboxes.ForEach(go => go.SetActiveSafe(false));
            }

            HasRemotePlayerTradeData = false;
        }

        private static void UpdateSocialButtons()
        {
            if (isBlockingRaft || isBlockingUser)
            {
                addAsFriendButton.SetActiveSafe(false);
                chatButton.SetActiveSafe(false);
                blockUserButton.SetActiveSafe(false);
                blockRaftButton.SetActiveSafe(false);
                confirmButton.SetActiveSafe(true);
                nevermindButton.SetActiveSafe(true);
                return;
            }

            confirmButton.SetActiveSafe(false);
            nevermindButton.SetActiveSafe(false);

            if (tradeSteamID == 0)
            {
                tradingWithText.GetComponent<Text>().text = "TRADING WITH NO ONE";
                addAsFriendButton.SetActiveSafe(false);
                chatButton.SetActiveSafe(false);
                blockUserButton.SetActiveSafe(false);
                blockRaftButton.SetActiveSafe(false);
                return;
            }

            blockUserButton.SetActiveSafe(true);

            CSteamID cTradeSteamID = new CSteamID(tradeSteamID);

            tradingWithText.GetComponent<Text>().text = "TRADING WITH " + SteamHelper.GetSteamUserName(cTradeSteamID, false);

            if (Semih_Network.IsHost && !buttonPositionsAreHost)
            {
                addAsFriendButton.transform.localPosition = new Vector3(-125f, -30f, 0f);
                chatButton.transform.localPosition = new Vector3(-125f, -30f, 0f);
                blockUserButton.transform.localPosition = new Vector3(0f, -30f, 0f);
                buttonPositionsAreHost = true;
            }
            else if (!Semih_Network.IsHost && buttonPositionsAreHost)
            {
                addAsFriendButton.transform.localPosition = new Vector3(-100f, -30f, 0f);
                chatButton.transform.localPosition = new Vector3(-100f, -30f, 0f);
                blockUserButton.transform.localPosition = new Vector3(100f, -30f, 0f);
                buttonPositionsAreHost = false;
            }

            blockRaftButton.SetActiveSafe(Semih_Network.IsHost);

            switch (SteamFriends.GetFriendRelationship(cTradeSteamID))
            {
                case EFriendRelationship.k_EFriendRelationshipFriend:
                    addAsFriendButton.SetActiveSafe(false);
                    chatButton.SetActiveSafe(true);
                    break;

                case EFriendRelationship.k_EFriendRelationshipIgnored: // How did we even get here???
                case EFriendRelationship.k_EFriendRelationshipIgnoredFriend:
                case EFriendRelationship.k_EFriendRelationshipRequestInitiator:
                    addAsFriendButton.SetActiveSafe(false);
                    chatButton.SetActiveSafe(false);
                    break;

                case EFriendRelationship.k_EFriendRelationshipNone:
                case EFriendRelationship.k_EFriendRelationshipBlocked:  // Blocked is not blocked, see https://github.com/maxvollmer/ISteamFriends-EFriendRelationship
                case EFriendRelationship.k_EFriendRelationshipRequestRecipient:
                    addAsFriendButton.SetActiveSafe(true);
                    chatButton.SetActiveSafe(false);
                    break;
            }
        }

        public static void UpdateRemoteTradeData(PlayerTradeData remotePlayerTradeData)
        {
            RemotePlayerTradeData = remotePlayerTradeData;
            HasRemotePlayerTradeData = true;
        }

        private static void FillSlotsWithItems(List<Slot> slots, List<SerializableData.Item> items)
        {
            var index = 0;
            for (; index < items.Count && index < slots.Count; index++)
            {
                var itemInstance = items[index];
                var itemBase = ItemManager.GetItemByIndex(itemInstance.uniqueIndex);
                slots[index].SetItem(new ItemInstance(itemBase, itemInstance.amount, itemInstance.uses));
            }
            for (; index < slots.Count; index++)
            {
                slots[index].SetItem(null);
            }
        }

        public static void CompleteTrade(List<SerializableData.Item> remoteItems)
        {
            tradeStopWatch.Stop();
            tradeStopWatch.Reset();

            FillSlotsWithItems(localSlots, remoteItems);
            FillSlotsWithItems(remoteSlots, new List<SerializableData.Item>());
            ReturnLocalSlotItemsToInventory();

            remoteCheckboxes.ForEach(go => go.SetActiveSafe(false));

            OnModify();

            ComponentManager<SoundManager>.Value.PlayUI_MoveItem();
        }


        public static bool IsOpen { get; private set; }
        public static long TimeSinceOpened { get { return timeSinceOpened.ElapsedMilliseconds; } }

        public static bool IsAcceptingTrade { get; private set; }
        private static PlayerTradeData RemotePlayerTradeData { get; set; }
        private static bool HasRemotePlayerTradeData { get; set; } = false;

        private static string RecurseParents(GameObject gameObject)
        {
            if (gameObject == null)
                return "";

            return gameObject + "(" + gameObject.transform.position + ", " + gameObject.transform.rotation.eulerAngles + ")" + " > " + RecurseParents(gameObject.transform.parent?.gameObject);
        }


        public static void Open(CanvasHelper canvas, CSteamID steamID)
        {
            if (IsOpen)
            {
                Close();
            }

            if (!canvas.OpenMenuCloseOther(MenuType.Inventory, false))
                return;

            tradeSteamID = steamID.m_SteamID;

            CanvasHelper.ActiveMenu = (MenuType)1337;

            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            localPlayer.PersonController.IsMovementFree = false;

            if (tradeInventoryGameObject == null)
            {
                var playerInventory = localPlayer.Inventory;

                tradeInventoryGameObject = new GameObject("TradeInventory", typeof(RectTransform), typeof(CanvasRenderer));
                var tradeInventoryTransform = tradeInventoryGameObject.transform as RectTransform;
                tradeInventoryTransform.SetPositionAndRotation(new Vector3(410f, 450f, 0f), Quaternion.identity);
                tradeInventoryTransform.SetParent(playerInventory.gameObject.transform.parent, false);
                tradeInventoryTransform.sizeDelta = new Vector2(0f, 0f);
                tradeInventoryTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

                tradeInventoryTransform.anchorMax = new Vector2(0.5f, 0.5f);

                CreateBackground(tradeInventoryGameObject, 0f, 50f, 420f, 950f).transform.SetAsFirstSibling();

                tradingWithText = AddText(tradeInventoryGameObject, "TRADING WITH [UNKNOWN]", 20f, 30);

                AddText(tradeInventoryGameObject, "YOUR OFFER", -100f);
                AddDivider(tradeInventoryGameObject, -150f);

                AddText(tradeInventoryGameObject, "THEIR OFFER", -450f);
                AddDivider(tradeInventoryGameObject, -500f);

                AddDivider(tradeInventoryGameObject, -800f);

                waitingText = AddText(tradeInventoryGameObject, "Waiting for remote player to accept", -880f, 15);

                addAsFriendButton = AddButton(tradeInventoryGameObject, "Add Steam Friend", OnAddSteamFriend, -125f, -30f);
                chatButton = AddButton(tradeInventoryGameObject, "Open Chat", OnChat, -125f, -30f);
                blockUserButton = AddButton(tradeInventoryGameObject, "Block Player", OnBlockUser, 0f, -30f);
                blockRaftButton = AddButton(tradeInventoryGameObject, "Block Raft", OnBlockRaft, 125f, -30f);
                confirmButton = AddButton(tradeInventoryGameObject, "Confirm Block", OnConfirm, -100f, -30f);
                nevermindButton = AddButton(tradeInventoryGameObject, "Never Mind", OnNevermind, 100f, -30f);

                buttonPositionsAreHost = true;

                acceptButton = AddButton(tradeInventoryGameObject, "Accept", OnAccept, -100f, -840f);
                modifyButton = AddButton(tradeInventoryGameObject, "Modify", OnModify, -100f, -840f);
                AddButton(tradeInventoryGameObject, "Close", OnClose, 100f, -840f);

                localSlots = CreateSlots(tradeInventoryGameObject, -170f);
                remoteSlots = CreateSlots(tradeInventoryGameObject, -520f);

                localSlots.ForEach(slot => acceptCheckboxes.Add(AddCheckbox(slot.gameObject)));
                remoteSlots.ForEach(slot => remoteCheckboxes.Add(AddCheckbox(slot.gameObject)));

                remoteCheckboxes.ForEach(go => go.SetActiveSafe(false));

                tradeInventory = tradeInventoryGameObject.AddComponent<TradeInventory>();

                wishList = CreateWishList(tradeInventoryGameObject.transform.parent.gameObject, "Your Wishlist", wishListSlots, -50f, false);
                remoteWishList = CreateWishList(tradeInventoryGameObject.transform.parent.gameObject, "Their Wishlist", remoteWishListSlots, -500f, false);

                wishListSelector = CreateWishList(tradeInventoryGameObject.transform.parent.gameObject, "Select Item", wishListSelectorSlots, 0f, true);
            }

            updateSocialButtonsStopwatch.Stop();
            updateSocialButtonsStopwatch.Reset();

            UpdateSocialButtons();
            OnModify();

            wishList.SetActiveSafe(true);
            remoteWishList.SetActiveSafe(true);
            wishListSelector.SetActiveSafe(false);
            wishListSelectorSlot = null;

            localPlayer.Inventory.secondInventory = tradeInventory;
            tradeInventory.secondInventory = localPlayer.Inventory;

            tradeInventoryGameObject.SetActiveSafe(true);
            PlayerItemManager.IsBusy = true;

            IsOpen = true;
            timeSinceOpened.Restart();
        }

        private static void OnConfirm()
        {
            if (isBlockingRaft)
            {
                if (Semih_Network.IsHost && RemoteSession.IsConnectedToPlayer)
                {
                    SettingsManager.BlockRaft(RemoteSession.ConnectedPlayer.m_SteamID, RemoteSession.ConnectedSessionID);
                    RemoteSession.Disconnect();
                }
                isBlockingRaft = false;
            }
            if (isBlockingUser)
            {
                if (tradeSteamID != 0)
                {
                    SettingsManager.BlockPlayer(tradeSteamID);
                    RemoteRaft.RemoveRemotePlayer(tradeSteamID);
                }
                Close();
                isBlockingUser = false;
            }
            updateSocialButtonsStopwatch.Stop();
        }

        private static void OnNevermind()
        {
            isBlockingRaft = false;
            isBlockingUser = false;
            updateSocialButtonsStopwatch.Stop();
        }

        private static void OnChat()
        {
            if (tradeSteamID != 0)
            {
                SteamHelper.OpenSteamChat(new CSteamID(tradeSteamID));
            }
        }

        private static void OnBlockRaft()
        {
            isBlockingRaft = true;
            updateSocialButtonsStopwatch.Stop();
        }

        private static void OnBlockUser()
        {
            isBlockingUser = true;
            updateSocialButtonsStopwatch.Stop();
        }

        private static void OnAddSteamFriend()
        {
            if (tradeSteamID != 0)
            {
                SteamHelper.AddSteamFriend(new CSteamID(tradeSteamID));
            }
        }

        private static void OnAccept()
        {
            if (tradeStopWatch.IsRunning)
                return;

            acceptButton.SetActiveSafe(false);
            modifyButton.SetActiveSafe(true);
            waitingText.SetActiveSafe(true);
            acceptCheckboxes.ForEach(go => go.SetActiveSafe(true));
            tradeInventory.DisableSlots();
            IsAcceptingTrade = true;
        }

        private static void OnModify()
        {
            if (tradeStopWatch?.IsRunning ?? false)
                return;

            acceptButton?.SetActiveSafe(true);
            modifyButton?.SetActiveSafe(false);
            waitingText?.SetActiveSafe(false);
            acceptCheckboxes?.ForEach(go => go.SetActiveSafe(false));
            tradeInventory?.EnableSlots();
            IsAcceptingTrade = false;
            TradeManager.AbandonTrade();
        }

        private static void OnClose()
        {
            if (tradeStopWatch.IsRunning)
                return;

            Close();
        }

        private static GameObject AddButton(GameObject gameObject, string text, UnityAction onClick, float x, float y)
        {
            var inventoryResearchTable = ComponentManager<Inventory_ResearchTable>.Value;
            var buttonPrefab = inventoryResearchTable.gameObject.transform.FindChildRecursively("ResearchButton").gameObject;
            if (buttonPrefab != null)
            {
                var button = UnityEngine.Object.Instantiate(buttonPrefab);
                I18NHelper.FixI18N(button, text);
                button.transform.position = new Vector3(x, y, 0f);
                ((RectTransform)button.transform).anchorMin = new Vector2(0f, 1f);
                ((RectTransform)button.transform).anchorMax = new Vector2(0f, 1f);
                button.transform.SetParent(gameObject.transform, false);
                button.GetComponentInChildren<Text>().text = text;
                button.GetComponent<Button>().onClick.RemoveAllListeners();
                button.GetComponent<Button>().onClick = new ButtonClickedEvent();
                button.GetComponent<Button>().onClick.AddListener(onClick);
                return button;
            }
            return null;
        }

        private static GameObject AddCheckbox(GameObject gameObject)
        {
            var inventoryResearchTable = ComponentManager<Inventory_ResearchTable>.Value;
            var checkboxPrefab = inventoryResearchTable.gameObject.transform.FindChildRecursively("ResearchedCheckbox").gameObject;
            if (checkboxPrefab != null)
            {
                var checkbox = UnityEngine.Object.Instantiate(checkboxPrefab);
                checkbox.transform.position = Vector3.zero;
                ((RectTransform)checkbox.transform).sizeDelta = Vector2.zero;
                checkbox.transform.SetParent(gameObject.transform, false);

                foreach (var image in checkbox.GetComponentsInChildren<Image>())
                {
                    var color = image.color;
                    image.color = new Color(color.r, color.g, color.b, 0.5f);
                }

                return checkbox;
            }
            return null;
        }

        private static GameObject CreateWishList(GameObject gameObject, string title, List<Slot> wishListSlots, float y, bool isSelector)
        {
            var wishList = new GameObject("TradeInventoryWishList", typeof(RectTransform), typeof(CanvasRenderer));
            ((RectTransform)wishList.transform).SetPositionAndRotation(new Vector3(780f, 450f + y, 0f), Quaternion.identity);
            wishList.transform.SetParent(gameObject.transform, false);

            CreateBackground(wishList, 0f, 0f, 240f, isSelector ? 925f : 300f);

            AddText(wishList, title, -20f, 20, 200f);

            if (isSelector)
            {
                int yindex = 0;
                foreach (var items in TradeItems.GetTradeItems())
                {
                    yindex = AddWishListItems(wishList, wishListSlots, items, yindex, 8) + 2;
                }
            }
            else
            {
                AddWishListItems(wishList, wishListSlots, new Item_Base[30], 0, 5);
            }

            wishList.AddComponent<WishListInventory>();
            return wishList;
        }

        private static int AddWishListItems(GameObject wishList, List<Slot> wishListSlots, IEnumerable<Item_Base> items, int yindex, int numSlotsPerRow)
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            var playerInventory = localPlayer.Inventory;

            var slotSize = 200f / numSlotsPerRow;

            int lastusedyindex = yindex;
            int xindex = 0;
            foreach (var item in items)
            {
                lastusedyindex = yindex;

                var slot = UnityEngine.Object.Instantiate(playerInventory.slotPrefab);
                slot.transform.position = new Vector3(xindex * (slotSize + 1) - 105f, -40f - yindex * (slotSize + 1), 0f);
                ((RectTransform)slot.transform).sizeDelta = new Vector2(slotSize, slotSize);
                slot.transform.SetParent(wishList.transform, false);
                if (item != null)
                {
                    slot.GetComponent<Slot>().AddItem(item, 1);
                }
                else
                {
                    slot.GetComponent<Slot>().SetItem(null);
                }
                slot.GetComponent<Slot>().sliderComponent.gameObject.SetActiveSafe(false);
                slot.GetComponent<Slot>().sliderComponent = null;

                wishListSlots.Add(slot.GetComponent<Slot>());

                xindex++;
                if (xindex == numSlotsPerRow)
                {
                    xindex = 0;
                    yindex++;
                }
            }
            return lastusedyindex;
        }

        private static void OpenWishListSelector(Slot slot)
        {
            wishList.SetActiveSafe(false);
            remoteWishList.SetActiveSafe(false);
            wishListSelector.SetActiveSafe(true);
            wishListSelectorSlot = slot;
        }

        private static void FinishWishListSelector(ItemInstance itemInstance)
        {
            if (wishListSelectorSlot != null)
            {
                wishListSelectorSlot.SetItem(itemInstance);
            }

            wishList.SetActiveSafe(true);
            remoteWishList.SetActiveSafe(true);
            wishListSelector.SetActiveSafe(false);
            wishListSelectorSlot = null;
        }

        private static List<Slot> CreateSlots(GameObject gameObject, float y)
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            var playerInventory = localPlayer.Inventory;

            List<Slot> slots = new List<Slot>();
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    var slot = UnityEngine.Object.Instantiate(playerInventory.slotPrefab);
                    slot.transform.position = new Vector3(i * 80f - 200f, y - j * 80f, 0f);
                    slot.transform.SetParent(gameObject.transform, false);
                    slots.Add(slot.GetComponent<Slot>());
                }
            }

            return slots;
        }

        private static GameObject CreateBackground(GameObject gameObject, float x, float y, float width, float height)
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            var playerInventory = localPlayer.Inventory;
            var backgroundPrefab = playerInventory.gameObject.transform.FindChildRecursively("BrownBackground").gameObject;
            if (backgroundPrefab != null)
            {
                var background = UnityEngine.Object.Instantiate(backgroundPrefab);
                background.transform.position = new Vector3(x, y, 0f);
                ((RectTransform)background.transform).sizeDelta = new Vector2(width, height);
                ((RectTransform)background.transform).anchorMin = new Vector2(0f, 1f);
                ((RectTransform)background.transform).anchorMax = new Vector2(0f, 1f);
                background.transform.SetParent(gameObject.transform, false);
                return background;
            }
            return null;
        }

        private static GameObject AddText(GameObject gameObject, string text, float y, int fontSize = 40, float width = 400f)
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            var playerInventory = localPlayer.Inventory;
            var textPrefab = playerInventory.gameObject.transform.FindChildRecursively("InventoryText").gameObject;
            if (textPrefab != null)
            {
                var textInstance = UnityEngine.Object.Instantiate(textPrefab);
                I18NHelper.FixI18N(textInstance, text);
                textInstance.transform.position = new Vector3(-width*0.5f, y, 0f);
                ((RectTransform)textInstance.transform).anchorMin = new Vector2(0f, 1f);
                ((RectTransform)textInstance.transform).anchorMax = new Vector2(0f, 1f);
                ((RectTransform)textInstance.transform).sizeDelta = new Vector2(width, 100f);
                textInstance.transform.SetParent(gameObject.transform, false);
                textInstance.GetComponent<Text>().text = text;
                textInstance.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                textInstance.GetComponent<Text>().fontSize = fontSize;
                textInstance.GetComponent<Text>().resizeTextMaxSize = fontSize;
                textInstance.GetComponent<Text>().resizeTextMinSize = Math.Min(textInstance.GetComponent<Text>().resizeTextMinSize, fontSize);
                return textInstance;
            }
            return null;
        }

        private static void AddDivider(GameObject gameObject, float y)
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            var playerInventory = localPlayer.Inventory;
            var dividerPrefab = playerInventory.gameObject.transform.FindChildRecursively("divider").gameObject;
            if (dividerPrefab != null)
            {
                var divider = UnityEngine.Object.Instantiate(dividerPrefab);
                divider.transform.position = new Vector3(0f, y, 0f);
                ((RectTransform)divider.transform).sizeDelta = new Vector2(380f, ((RectTransform)divider.transform).sizeDelta.y);
                ((RectTransform)divider.transform).anchorMin = new Vector2(0f, 1f);
                ((RectTransform)divider.transform).anchorMax = new Vector2(0f, 1f);
                divider.transform.SetParent(gameObject.transform, false);
            }
        }

        public static void Close()
        {
            if (!IsOpen)
                return;

            tradeSteamID = 0;

            tradeStopWatch.Stop();
            tradeStopWatch.Reset();
            TradeManager.AbandonTrade();

            OnNevermind();
            OnModify();
            ReturnLocalSlotItemsToInventory();

            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            localPlayer.PersonController.IsMovementFree = true;

            localPlayer.Inventory.secondInventory = null;
            tradeInventory.secondInventory = null;
            tradeInventoryGameObject.SetActiveSafe(false);

            wishList.SetActiveSafe(false);
            remoteWishList.SetActiveSafe(false);
            wishListSelector.SetActiveSafe(false);
            wishListSelectorSlot = null;

            CanvasHelper.ActiveMenu = MenuType.Inventory;
            ComponentManager<CanvasHelper>.Value.CloseMenu(MenuType.Inventory);

            PlayerItemManager.IsBusy = false;

            IsOpen = false;

            timeSinceOpened.Stop();
            timeSinceOpened.Reset();

            updateSocialButtonsStopwatch.Stop();
        }

        private static void ReturnLocalSlotItemsToInventory()
        {
            var localPlayer = ComponentManager<Semih_Network>.Value.GetLocalPlayer();
            foreach (var slot in localSlots)
            {
                if (slot.IsEmpty)
                    continue;

                localPlayer.Inventory.AddItem(slot.itemInstance, true);
                slot.SetItem(null);
            }
        }

        public static void Destroy()
        {
            Close();
            UnityEngine.Object.Destroy(tradeInventoryGameObject);
            UnityEngine.Object.Destroy(wishList);
            UnityEngine.Object.Destroy(remoteWishList);
            UnityEngine.Object.Destroy(wishListSelector);
            tradeInventoryGameObject = null;
            wishList = null;
            remoteWishList = null;
            wishListSelector = null;
            wishListSelectorSlot = null;
            tradeInventory = null;
            waitingText = null;
            addAsFriendButton = null;
            chatButton = null;
            blockUserButton = null;
            blockRaftButton = null;
            confirmButton = null;
            nevermindButton = null;
            acceptButton = null;
            modifyButton = null;
            localSlots.Clear();
            remoteSlots.Clear();
            wishListSlots.Clear();
            wishListSelectorSlots.Clear();
            acceptCheckboxes.Clear();
            remoteCheckboxes.Clear();
            timeSinceOpened.Stop();
            timeSinceOpened.Reset();
            tradeStopWatch.Stop();
            tradeStopWatch.Reset();
            RemotePlayerTradeData = null;
            HasRemotePlayerTradeData = false;
            tradeSteamID = 0;
            updateSocialButtonsStopwatch.Stop();
            isBlockingRaft = false;
            isBlockingUser = false;
        }
    }
}
