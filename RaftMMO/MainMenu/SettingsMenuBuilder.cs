using HarmonyLib;
using RaftMMO.ModEntry;
using RaftMMO.ModSettings;
using RaftMMO.Network;
using RaftMMO.Utilities;
using RaftMMO.World;
using Steamworks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using static UnityEngine.UI.Dropdown;
using static UnityEngine.UI.Toggle;

namespace RaftMMO.MainMenu
{
    public static class SettingsMenuBuilder
    {
        private enum RaftType
        {
            NORMAL,
            FAVORITE,
            BLOCKED
        }

        private enum PlayerType
        {
            NORMAL,
            BLOCKED
        }

        private static TabStuff metRaftEntriesTab = null;
        private static TabStuff favoritedRaftEntriesTab = null;
        private static TabStuff blockedRaftEntriesTab = null;
        private static TabStuff metPlayerEntriesTab = null;
        private static TabStuff blockedPlayerEntriesTab = null;
        private static TabStuff modSettingsTab = null;

        public static void Destroy()
        {
            metRaftEntriesTab = null;
            favoritedRaftEntriesTab = null;
            blockedRaftEntriesTab = null;
            metPlayerEntriesTab = null;
            blockedPlayerEntriesTab = null;
            modSettingsTab = null;
            PlayerCounter.updateNumbersOfPlayersCallback -= UpdateNumberOfRaftMMOPlayersText;
        }


        private static List<Text> _numberOfPlayersTexts = new List<Text>();

        private static void AddNumberOfRaftMMOPlayersText(Text text)
        {
            _numberOfPlayersTexts.Add(text);
            UpdateNumberOfRaftMMOPlayersText(PlayerCounter.NumberOfPlayers);
        }

        private static void UpdateNumberOfRaftMMOPlayersText(int numberOfPlayers)
        {
            _numberOfPlayersTexts = _numberOfPlayersTexts.Where(t => !t.IsDestroyed()).ToList();

            foreach (var numberOfPlayersText in _numberOfPlayersTexts)
            {
                numberOfPlayersText.text = " Number of RaftMMO rafts currently sailing the seas: " + numberOfPlayers;
            }
        }


        public static void BuildSettingsMenu(GameObject optionMenuParent, GameObject parent)
        {
            PlayerCounter.updateNumbersOfPlayersCallback += UpdateNumberOfRaftMMOPlayersText;

            var optionMenuCopy = Object.Instantiate(optionMenuParent);
            optionMenuCopy.SetActive(true);
            var optionsCanvasCopyTransform = optionMenuCopy.transform as RectTransform;
            optionsCanvasCopyTransform.SetParent(parent.transform, false);
            optionsCanvasCopyTransform.sizeDelta = new Vector2(optionsCanvasCopyTransform.sizeDelta.x, optionsCanvasCopyTransform.sizeDelta.y - 100f);

            Object.Destroy(optionMenuCopy.transform.FindChildRecursively("BrownBackground").gameObject);
            Object.Destroy(optionMenuCopy.transform.FindChildRecursively("CloseButton").gameObject);
            Object.Destroy(optionMenuCopy.transform.FindChildRecursively("OptionsText").gameObject);

            int i = 0;
            var statusTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Status", i++);
            metRaftEntriesTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Met Rafts", i++);
            favoritedRaftEntriesTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Favorite Rafts", i++);
            blockedRaftEntriesTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Blocked Rafts", i++);
            metPlayerEntriesTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Met Players", i++);
            blockedPlayerEntriesTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Blocked Players", i++);
            modSettingsTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "Mod Settings", i++);
            var aboutTab = TabCopyHelper.CopyTab(optionMenuCopy, "Graphics", "About", i++);

            TabCopyHelper.DestroyTab(optionMenuCopy, "General");
            TabCopyHelper.DestroyTab(optionMenuCopy, "Controls");
            TabCopyHelper.DestroyTab(optionMenuCopy, "Graphics");
            TabCopyHelper.DestroyTab(optionMenuCopy, "Audio");
            TabCopyHelper.DestroyTab(optionMenuCopy, "RaftMMO");

            RefreshRaftEntries();
            RefreshPlayerEntries();
            RefreshModSettings();

            MakeAbout(aboutTab, optionMenuParent);
            MakeStatus(statusTab, optionMenuParent);

            var tabGroup = optionMenuCopy.GetComponentInChildren<TabGroup>();
            Traverse.Create(tabGroup).Field("tabButtons").SetValue(tabGroup.GetComponentsInChildren<TabButton>());
            Traverse.Create(tabGroup).Field("selectedTabButton").SetValue(null);
            tabGroup.SelectTab(0);

            var tabContainer = optionMenuCopy.transform.FindChildRecursively("TabContainer").gameObject;
            var tabContainerTransform = tabContainer.transform as RectTransform;
            tabContainerTransform.localPosition = new Vector3(30f, tabContainerTransform.localPosition.y - 10f);
            tabContainerTransform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            tabContainerTransform.pivot = new Vector2(0f, 0.5f);

            tabContainer.transform.parent.Find("Divider").localScale = new Vector3(1f, 0.75f, 1f);

            var horizontalLayoutGroup = tabContainer.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.spacing = 0f;
            horizontalLayoutGroup.childForceExpandWidth = false;
            var contentSizeFitter = horizontalLayoutGroup.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void MakeListOfRafts(GameObject templates, GameObject container, RaftType raftType)
        {
            container.DestroyAllChildren();

            RaftEntry[] raftEntries;

            switch (raftType)
            {
                case RaftType.NORMAL:
                    CreateText(templates, container, "Here you can see all rafts you have met so far. You can favorite or block rafts from here.");
                    raftEntries = SettingsManager.GetMetRaftsByDate.ToArray();
                    break;
                case RaftType.FAVORITE:
                    CreateText(templates, container, "These are all rafts you have favorited. Favorited rafts have a higher chance to appear near buoys.");
                    raftEntries = SettingsManager.GetFavoritedRaftsByDate.ToArray();
                    break;
                case RaftType.BLOCKED:
                    CreateText(templates, container, "These are all rafts you have blocked. You can unblock them from here.");
                    raftEntries = SettingsManager.GetBlockedRaftsByDate.ToArray();
                    break;
                default:
                    return;
            }

            CreateSpacer(templates, container, 24f);

            if (raftEntries.Length == 0)
            {
                switch (raftType)
                {
                    case RaftType.NORMAL:
                        if (SettingsManager.Settings.MetRafts.Count == 0)
                        {
                            CreateText(templates, container, "(You haven't met any rafts yet.)");
                        }
                        else
                        {
                            CreateText(templates, container, "(You have favorited or blocked all rafts you have met.)");
                        }
                        break;
                    case RaftType.FAVORITE:
                        CreateText(templates, container, "(You haven't favorited any rafts yet.)");
                        break;
                    case RaftType.BLOCKED:
                        CreateText(templates, container, "(You haven't blocked any rafts yet.)");
                        break;
                }
            }
            else
            {
                for (var i = 0; i < raftEntries.Length; i++)
                {
                    CreateRaftEntry(raftEntries[i], templates, container, i % 2 == 0);
                }
            }
        }

        private static void MakeListOfPlayers(GameObject templates, GameObject container, PlayerType playerType)
        {
            container.DestroyAllChildren();

            var numPlayersText = CreateText(templates, container, "Number of RaftMMO rafts currently sailing the seas: ...");
            AddNumberOfRaftMMOPlayersText(numPlayersText.GetComponent<Text>());
            CreateSpacer(templates, container, 24f);

            PlayerEntry[] playerEntries;

            switch (playerType)
            {
                case PlayerType.NORMAL:
                    CreateText(templates, container, "Here you can see all players you have met so far. You can add them as Steam friends or block them from here.");
                    playerEntries = SettingsManager.GetMetPlayersByDate.ToArray();
                    break;
                case PlayerType.BLOCKED:
                    CreateText(templates, container, "These are all players you have blocked. You can unblock them from here.");
                    playerEntries = SettingsManager.GetBlockedPlayersByDate.ToArray();
                    break;
                default:
                    return;
            }

            CreateSpacer(templates, container, 24f);

            if (playerEntries.Length == 0)
            {
                switch (playerType)
                {
                    case PlayerType.NORMAL:
                        CreateText(templates, container, "(You haven't met any players yet.)");
                        break;
                    case PlayerType.BLOCKED:
                        CreateText(templates, container, "(You haven't blocked any players yet.)");
                        break;
                }
            }
            else
            {
                for (var i = 0; i < playerEntries.Length; i++)
                {
                    CreatePlayerEntry(playerEntries[i], templates, container, i % 2 == 0);
                }
            }
        }

        private static void MakeAbout(TabStuff tab, GameObject templates)
        {
            CreateText(templates, tab.content, "Hi, I am Max! I make RaftMMO.");
            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "If you like my work, please consider supporting me on Patreon:");
            CreateButton(templates, tab.content, "Patreon", () => Application.OpenURL("https://www.patreon.com/maxmakesmods"));

            CreateText(templates, tab.content, "Also check out my Discord:");
            CreateButton(templates, tab.content, "Max Makes Mods", () => Application.OpenURL("https://discord.gg/jujwEGf62K"));

            CreateSpacer(templates, tab.content, 24);
            CreateText(templates, tab.content, "This mod spawns buoys in the ocean, at which you can meet other rafts.");
            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "Buoys can be located with the receiver, they appear as red dots on the display.");
            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "You can trade with players from other rafts. Fighting, stealing, and sabotaging other rafts is not possible.");
            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "Buoys always spawn away from islands. If you sail too far from the buoy, the other raft will not be able to follow you.");

            CreateSpacer(templates, tab.content, 24);
            CreateText(templates, tab.content, "Visit the mod on RaftModding:");
            CreateButton(templates, tab.content, "RaftModding", () => Application.OpenURL("https://www.raftmodding.com/mods/raftmmo"));

            CreateText(templates, tab.content, "Visit the mod on Nexus Mods:");
            CreateButton(templates, tab.content, "Nexus Mods", () => Application.OpenURL("https://www.nexusmods.com/raft/mods/5"));

            CreateText(templates, tab.content, "The mod is open source:");
            CreateButton(templates, tab.content, "Github", () => Application.OpenURL("https://github.com/maxvollmer/RaftMMO"));

            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "I occasionally post WIP videos of my projects on Youtube:");
            CreateButton(templates, tab.content, "Youtube", () => Application.OpenURL("https://www.youtube.com/MaxMakesMods"));

            CreateSpacer(templates, tab.content, 12);
            CreateText(templates, tab.content, "Other projects I make that people like:");
            CreateButton(templates, tab.content, "DeepWoods for SDV", () => Application.OpenURL("https://www.nexusmods.com/stardewvalley/mods/2571"));

            CreateSpacer(templates, tab.content, 12);
            CreateButton(templates, tab.content, "Half-Life: VR", () => Application.OpenURL("https://www.halflifevr.de"));
        }

        private static void MakeStatus(TabStuff tab, GameObject templates)
        {
            if (CommonEntry.ModDataGetter == null)
                return;

            CreateText(templates, tab.content, "RaftMMO Status:");
            CreateSpacer(templates, tab.content, 6f);

            var numPlayersText = CreateText(templates, tab.content, "Number of RaftMMO rafts currently sailing the seas: ...");
            AddNumberOfRaftMMOPlayersText(numPlayersText.GetComponent<Text>());
            CreateSpacer(templates, tab.content, 24f);

            CreateText(templates, tab.content, "Meeting Status:");
            CreateSpacer(templates, tab.content, 6f);

            if (RemoteSession.IsConnectedToPlayer || ClientSession.IsHostConnectedToPlayer)
            {
                CreateText(templates, tab.content, "You are currently meeting another raft!");
            }
            else if (Raft_Network.InMenuScene)
            {
                CreateText(templates, tab.content, "You are not currently in a game.");
            }
            else if (Raft_Network.IsHost)
            {
                if (RemoteSession.IsInCoolDown(out long remainingMilliSeconds))
                {
                    long seconds = System.Math.Max((long)System.Math.Ceiling(remainingMilliSeconds / 1000d), 1);
                    CreateText(templates, tab.content, $"You just met someone and are in meeting cooldown for another {seconds} seconds.\nMeeting cooldowns ensure everyone gets a chance to meet someone.");
                }
                else if (BuoyManager.IsCloseEnoughToConnect())
                {
                    CreateText(templates, tab.content, "You are in the open sea and able to meet another raft.");
                }
                else if (Globals.CurrentRaftMeetingPointNumIslandsInWay > 0)
                {
                    CreateText(templates, tab.content, $"You are too close to {Globals.CurrentRaftMeetingPointNumIslandsInWay} islands to meet another raft.");
                }
                else
                {
                    CreateText(templates, tab.content, $"You are currently unable to meet other rafts.\nTry to sail away from islands. See the log for details if this status doesn't change.");
                }
            }
            else
            {
                CreateText(templates, tab.content, $"You are not currently meeting another raft.\nA detailed meeting status is shown to the game host.");
            }

            CreateSpacer(templates, tab.content, 24f);
        }

        private static GameObject CreateRaftEntry(RaftEntry raftEntry, GameObject templates, GameObject content, bool background)
        {
            var raftEntryObject = new GameObject("RaftEntry", typeof(RectTransform), typeof(CanvasRenderer));
            var raftEntryTransform = raftEntryObject.transform as RectTransform;
            raftEntryTransform.sizeDelta = new Vector2(0f, 120f);
            raftEntryTransform.anchorMin = new Vector2(0f, 1f);
            raftEntryTransform.anchorMax = new Vector2(0f, 1f);
            raftEntryTransform.pivot = new Vector2(0f, 1f);
            raftEntryTransform.SetParent(content.transform, false);

            string userName = SteamHelper.GetSteamUserName(new CSteamID(raftEntry.steamID), false);

            CreateImage(raftEntryObject, ImageLoader.LoadFileImage(SettingsManager.GetScreenshotPath(raftEntry.steamID, raftEntry.sessionID)), 10f, 10f, 100f, 100f);

            CreateText(templates, raftEntryObject, "Raft by " + userName + " (SteamID: " + SteamHelper.GetSteamIDDisplayString(new CSteamID(raftEntry.steamID)) + ")", 120f, 10f);
            CreateText(templates, raftEntryObject, raftEntry.numPlayers + " players. Met " + raftEntry.metTimes + " times. Last Met: "+ CreateLastMetTimeString(raftEntry.steamID, raftEntry.sessionID, raftEntry.lastMet), 120f, 40f);

            if (raftEntry.isFavorite)
            {
                CreateButton(templates, raftEntryObject, "Remove from Favorites", () => { SettingsManager.FavoriteRaft(raftEntry.steamID, raftEntry.sessionID, true); }, 120f, 80f, 0.5f);
            }
            else
            {
                CreateButton(templates, raftEntryObject, "Add to Favorites", () => { SettingsManager.FavoriteRaft(raftEntry.steamID, raftEntry.sessionID); }, 120f, 80f, 0.5f);
            }
            if (raftEntry.isBlocked)
            {
                CreateButton(templates, raftEntryObject, "Unblock", () => { SettingsManager.BlockRaft(raftEntry.steamID, raftEntry.sessionID, true); }, 280f, 80f, 0.5f);
            }
            else
            {
                CreateButton(templates, raftEntryObject, "Block", () => { SettingsManager.BlockRaft(raftEntry.steamID, raftEntry.sessionID); }, 280f, 80f, 0.5f);
            }

            if (!IsMeetingThisRaftRightNow(raftEntry.steamID, raftEntry.sessionID))
            {
                CreateButton(templates, raftEntryObject, "Forget this Raft", () => { SettingsManager.ForgetRaft(raftEntry.steamID, raftEntry.sessionID); }, 440f, 80f, 0.5f);
            }

            if (background)
            {
                CreateBackground(templates, raftEntryObject);
            }

            return raftEntryObject;
        }

        private static GameObject CreatePlayerEntry(PlayerEntry playerEntry, GameObject templates, GameObject content, bool background)
        {
            var playerEntryObject = new GameObject("PlayerEntry", typeof(RectTransform), typeof(CanvasRenderer));
            var playerEntryTransform = playerEntryObject.transform as RectTransform;
            playerEntryTransform.sizeDelta = new Vector2(0f, 120f);
            playerEntryTransform.anchorMin = new Vector2(0f, 1f);
            playerEntryTransform.anchorMax = new Vector2(0f, 1f);
            playerEntryTransform.pivot = new Vector2(0f, 1f);
            playerEntryTransform.SetParent(content.transform, false);

            string userName = SteamHelper.GetSteamUserName(new CSteamID(playerEntry.steamID), false);

            CreateImage(playerEntryObject, playerEntry.model == 0 ? ImageLoader.MayaImage : ImageLoader.RouhiImage, 10f, 10f, 100f, 100f);

            CreateText(templates, playerEntryObject, userName + " (SteamID: " + SteamHelper.GetSteamIDDisplayString(new CSteamID(playerEntry.steamID)) + ")", 120f, 10f);
            CreateText(templates, playerEntryObject,
                "Met " + playerEntry.metTimes+ " times." +
                " Traded " + playerEntry.tradedTimes + " times." +
                " Seen on " + playerEntry.rafts.Count + " rafts." +
                " Last Met: " + CreateLastMetTimeString(playerEntry.steamID, playerEntry.lastMet), 120f, 40f);

            if (SteamFriends.GetFriendRelationship(new CSteamID(playerEntry.steamID)) == EFriendRelationship.k_EFriendRelationshipFriend)
            {
                CreateButton(templates, playerEntryObject, "Chat on Steam", () => { SteamHelper.OpenSteamChat(new CSteamID(playerEntry.steamID)); }, 120f, 80f, 0.5f);
            }
            else
            {
                CreateButton(templates, playerEntryObject, "Add Steam friend", () => { SteamHelper.AddSteamFriend(new CSteamID(playerEntry.steamID)); }, 120f, 80f, 0.5f);
            }

            if (playerEntry.isBlocked)
            {
                CreateButton(templates, playerEntryObject, "Unblock", () => { SettingsManager.BlockPlayer(playerEntry.steamID, true); }, 280f, 80f, 0.5f);
            }
            else
            {
                CreateButton(templates, playerEntryObject, "Block", () => { SettingsManager.BlockPlayer(playerEntry.steamID); }, 280f, 80f, 0.5f);
            }

            if (!RemoteRaft.IsRemotePlayer(playerEntry.steamID))
            {
                CreateButton(templates, playerEntryObject, "Forget this Player", () => { SettingsManager.ForgetPlayer(playerEntry.steamID); }, 440f, 80f, 0.5f);
            }

            if (background)
            {
                CreateBackground(templates, playerEntryObject);
            }

            return playerEntryObject;
        }

        private static bool IsMeetingThisRaftRightNow(ulong steamID, string sessionID)
        {
            return (Raft_Network.IsHost && RemoteSession.IsConnectedPlayer(new CSteamID(steamID)) && RemoteSession.ConnectedSessionID == sessionID)
                   || (!Raft_Network.IsHost && SteamHelper.IsSameSteamID(ClientSession.ConnectedSteamID, steamID) && ClientSession.ConnectedSessionID == sessionID);
        }

        private static string CreateLastMetTimeString(ulong playerSteamID, long lastMet)
        {
            if (RemoteRaft.IsRemotePlayer(playerSteamID))
            {
                return "Meeting right now!";
            }
            return CreateLastMetTimeString(lastMet);
        }

        private static string CreateLastMetTimeString(ulong sessionSteamID, string sessionID, long lastMet)
        {
            if (IsMeetingThisRaftRightNow(sessionSteamID, sessionID))
            {
                return "Meeting right now!";
            }
            return CreateLastMetTimeString(lastMet);
        }

        private static string CreateLastMetTimeString(long lastMet)
        {
            long seconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastMet;
            if (seconds < 60)
            {
                return seconds + " seconds ago.";
            }

            long minutes = seconds / 60;
            if (minutes < 60)
            {
                return minutes + " minutes ago.";
            }

            long hours = minutes / 60;
            if (hours < 24)
            {
                return hours + " hours ago.";
            }

            long days = hours / 24;
            return days + " days ago.";
        }

        private static void CreateImage(GameObject parent, Sprite sprite, float x, float y, float width, float height)
        {
            var image = new GameObject("Screenshot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var imageTransform = image.transform as RectTransform;
            imageTransform.anchorMin = Vector2.zero;
            imageTransform.anchorMax = Vector2.zero;
            imageTransform.pivot = Vector2.zero;
            imageTransform.localPosition = new Vector3(x, y, 0f);
            imageTransform.sizeDelta = new Vector2(width, height);
            imageTransform.SetParent(parent.transform, false);
            image.GetComponent<Image>().sprite = sprite;
        }

        private static GameObject CreateSpacer(GameObject templates, GameObject content, float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(CanvasRenderer));
            (spacer.transform as RectTransform).sizeDelta = new Vector2(0f, height);
            spacer.transform.SetParent(content.transform, false);
            return spacer;
        }

        private static GameObject CreateText(GameObject templates, GameObject content, string text, float x = 0f, float y = 0f)
        {
            var lineCount = System.Math.Max(1, text.Split('\n').Length);

            // text is cut off a bit at the left, can't figure out why, so I'll just add a space for each line to "fix" it...
            text = " " + text.Replace("\n", "\n ");

            var textTemplate = templates.transform.FindChildRecursively("OptionsText").gameObject;
            var textCopy = Object.Instantiate(textTemplate);
            I18NHelper.FixI18N(textCopy, text);
            var textTransform = textCopy.transform as RectTransform;
            textTransform.SetParent(content.transform, false);
            textTransform.anchorMin = new Vector2(0f, 1f);
            textTransform.anchorMax = new Vector2(0f, 1f);
            textTransform.pivot = new Vector2(0f, 1f);
            textTransform.localPosition = new Vector3(x, -y, 0f);
            textTransform.sizeDelta = new Vector2(10000f, 24f * lineCount);
            textCopy.GetComponent<Text>().text = text;
            textCopy.GetComponent<Text>().fontSize = 24;
            textCopy.SetActive(true);
            return textCopy;
        }

        private static void CreateBackground(GameObject templates, GameObject parent)
        {
            var imagePrefab = templates.transform.FindChildRecursively("ResetKeybinds").GetComponent<Image>();
            var image = parent.AddComponent<Image>();
            image.material = imagePrefab.material;
            image.color = imagePrefab.color;
            image.sprite = imagePrefab.sprite;
            image.overrideSprite = imagePrefab.overrideSprite;
            image.fillMethod = imagePrefab.fillMethod;
            image.type = imagePrefab.type;
        }

        private static GameObject CreateButton(GameObject templates, GameObject content, string text, UnityAction onClick, float x = 0f, float y = 0f, float scale = 0.75f)
        {
            var buttonPrefab = templates.transform.FindChildRecursively("ResetKeybinds").gameObject;
            var button = Object.Instantiate(buttonPrefab);
            I18NHelper.FixI18N(button, text);

            var buttonTransform = button.transform as RectTransform;
            buttonTransform.SetParent(content.transform, false);
            buttonTransform.anchorMin = new Vector2(0f, 1f);
            buttonTransform.anchorMax = new Vector2(0f, 1f);
            buttonTransform.pivot = new Vector2(0f, 1f);
            buttonTransform.localPosition = new Vector3(x, -y, 0f);
            buttonTransform.localScale = new Vector3(1f, scale, 1f);

            button.GetComponentInChildren<Text>().text = text;
            button.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            button.GetComponentInChildren<Button>().onClick = new ButtonClickedEvent();
            button.GetComponentInChildren<Button>().onClick.AddListener(onClick);
            button.GetComponentInChildren<Button>().gameObject.transform.localScale = new Vector3(scale, 1f, 1f);

            Object.Destroy(button.GetComponent<Image>());

            return button;
        }

        public static void RefreshRaftEntries()
        {
            var optionMenuParent = SettingsMenuInjector.GetOptionMenuParent();

            MakeListOfRafts(optionMenuParent, metRaftEntriesTab.content, RaftType.NORMAL);
            MakeListOfRafts(optionMenuParent, favoritedRaftEntriesTab.content, RaftType.FAVORITE);
            MakeListOfRafts(optionMenuParent, blockedRaftEntriesTab.content, RaftType.BLOCKED);
        }

        public static void RefreshPlayerEntries()
        {
            var optionMenuParent = SettingsMenuInjector.GetOptionMenuParent();

            MakeListOfPlayers(optionMenuParent, metPlayerEntriesTab.content, PlayerType.NORMAL);
            MakeListOfPlayers(optionMenuParent, blockedPlayerEntriesTab.content, PlayerType.BLOCKED);
        }

        public static void RefreshModSettings()
        {
            var optionMenuParent = SettingsMenuInjector.GetOptionMenuParent();

            var container = modSettingsTab.content;
            container.DestroyAllChildren();

            CreateText(optionMenuParent, container, "General");
            CreateDivider(optionMenuParent, container);

            CreateCheckbox(optionMenuParent, container, "Only Meet Steam Friends", false, SettingsManager.Settings.OnlyMeetSteamFriends, (bool enabled) => {
                SettingsManager.Settings.OnlyMeetSteamFriends = enabled;
                SettingsSaver.IsDirty = true;
            });

            CreateCheckbox(optionMenuParent, container, "Enable Buoy Smoke", false, SettingsManager.Settings.EnableBuoySmoke, (bool enabled) => {
                SettingsManager.Settings.EnableBuoySmoke = enabled;
                SettingsSaver.IsDirty = true;
            });

            CreateSpacer(optionMenuParent, container, 20f);
            CreateText(optionMenuParent, container, "Logging");
            CreateDivider(optionMenuParent, container);

            CreateDropdown(optionMenuParent, container, "Console Log Level", (int level) => {
                SettingsManager.Settings.LogLevel = (LogLevel)level;
                SettingsSaver.IsDirty = true;
            },
            (int)SettingsManager.Settings.LogLevel,
            LogLevel.ERROR.ToString(), LogLevel.WARNING.ToString(), LogLevel.DEBUG.ToString(), LogLevel.INFO.ToString());

            CreateCheckbox(optionMenuParent, container, "Enable Verbose Logging", false, SettingsManager.Settings.LogVerbose, (bool enabled) => {
                SettingsManager.Settings.LogVerbose = enabled;
                SettingsSaver.IsDirty = true;
            });

            CreateSpacer(optionMenuParent, container, 20f);
            CreateDivider(optionMenuParent, container);
            CreateText(optionMenuParent, container, "If there is a setting you'd like to see here, please let me know and I'll consider adding it in. Thanks!");

            CreateSpacer(optionMenuParent, container, 20f);
            CreateButton(optionMenuParent, container, "Open RaftMMO Settings Folder", () =>
            {
                try
                {
                    Process.Start(SettingsSaver.SavePath);
                }
                catch (System.Exception) { }
            });
        }

        private static void CreateCheckbox(GameObject templates, GameObject content, string label, bool background, bool enabled, UnityAction<bool> onValueChanged)
        {
            var checkboxPrefab = templates.transform.FindChildRecursively(background ? "Quick build" : "ShowPlayerNames").gameObject;
            var checkbox = Object.Instantiate(checkboxPrefab);
            I18NHelper.FixI18N(checkbox, label);

            checkbox.GetComponentInChildren<Text>().text = label;
            checkbox.GetComponentInChildren<Toggle>().onValueChanged.RemoveAllListeners();
            checkbox.GetComponentInChildren<Toggle>().onValueChanged = new ToggleEvent();
            checkbox.GetComponentInChildren<Toggle>().isOn = enabled;
            checkbox.GetComponentInChildren<Toggle>().onValueChanged.AddListener(onValueChanged);

            checkbox.transform.SetParent(content.transform, false);
        }

        private static void CreateDropdown(GameObject templates, GameObject content, string label, UnityAction<int> onValueChanged, int selectedoption, params string[] options)
        {
            var dropdownPrefab = templates.transform.FindChildRecursively("Language").gameObject;
            var dropdown = Object.Instantiate(dropdownPrefab);
            I18NHelper.FixI18N(dropdown, label);

            Object.Destroy(dropdown.GetComponentInChildren<I2.Loc.SetLanguageDropdown>());

            dropdown.transform.FindChildRecursively("Text").GetComponent<Text>().text = label;
            dropdown.GetComponentInChildren<Dropdown>().options = options.Select(o => new OptionData(o)).ToList();
            dropdown.GetComponentInChildren<Dropdown>().onValueChanged.RemoveAllListeners();
            dropdown.GetComponentInChildren<Dropdown>().onValueChanged = new DropdownEvent();
            dropdown.GetComponentInChildren<Dropdown>().value = selectedoption;
            dropdown.GetComponentInChildren<Dropdown>().onValueChanged.AddListener(onValueChanged);

            dropdown.transform.SetParent(content.transform, false);
        }

        private static void CreateDivider(GameObject templates, GameObject content)
        {
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer));
            (divider.transform as RectTransform).sizeDelta = new Vector2(0f, 5f);
            divider.transform.SetParent(content.transform, false);

            var dividerPrefab = templates.transform.FindChildRecursively("Divider").GetComponent<Image>();

            var image = divider.AddComponent<Image>();
            image.material = dividerPrefab.material;
            image.color = dividerPrefab.color;
            image.sprite = dividerPrefab.sprite;
            image.overrideSprite = dividerPrefab.overrideSprite;
            image.fillMethod = dividerPrefab.fillMethod;
            image.type = dividerPrefab.type;
        }
    }
}
