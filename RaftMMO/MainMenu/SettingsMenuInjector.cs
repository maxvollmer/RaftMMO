using HarmonyLib;
using RaftMMO.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace RaftMMO.MainMenu
{
    public class SettingsMenuInjector
    {
        private static TabStuff raftMMOTab = null;

        public static GameObject GetOptionMenuParent()
        {
            return Traverse.Create(ComponentManager<Settings>.Value).Field("optionsCanvas").GetValue<GameObject>().transform.FindChildRecursively("OptionMenuParent").gameObject;
        }

        public static void Inject()
        {
            SettingsMenuBuilder.Destroy();

            if (raftMMOTab != null)
            {
                Remove();
            }

            var optionMenuParent = GetOptionMenuParent();

            raftMMOTab = TabCopyHelper.CopyTab(optionMenuParent, "Graphics", "RaftMMO", 4);

            SettingsMenuBuilder.BuildSettingsMenu(optionMenuParent, raftMMOTab.content);

            var backgroundTransform = optionMenuParent.transform.FindChildRecursively("BrownBackground").transform as RectTransform;
            backgroundTransform.anchorMax = new Vector2(1.1f, 1f);

            var dividerTransform = optionMenuParent.transform.FindChildRecursively("Divider").transform as RectTransform;
            dividerTransform.anchorMax = new Vector2(1.1f, 1f);

            var tabContentTransform = optionMenuParent.transform.FindChildRecursively("TabContent").transform as RectTransform;
            tabContentTransform.anchorMax = new Vector2(1.1f, 1f);

            var closeButtonTransform = optionMenuParent.transform.FindChildRecursively("CloseButton").transform as RectTransform;
            closeButtonTransform.anchorMin = new Vector2(1.1f, 1f);
            closeButtonTransform.anchorMax = new Vector2(1.1f, 1f);
        }

        public static void Remove()
        {
            SettingsMenuBuilder.Destroy();

            if (raftMMOTab == null)
                return;

            var optionMenuParent = GetOptionMenuParent();

            var tabGroup = optionMenuParent.GetComponentInChildren<TabGroup>();

            if (tabGroup.SelectedTabButton == raftMMOTab.tabButton)
            {
                tabGroup.SelectTab(0);
            }

            var backgroundTransform = optionMenuParent.transform.FindChildRecursively("BrownBackground").transform as RectTransform;
            backgroundTransform.anchorMax = new Vector2(1f, 1f);

            var dividerTransform = optionMenuParent.transform.FindChildRecursively("Divider").transform as RectTransform;
            dividerTransform.anchorMax = new Vector2(1f, 1f);

            var tabContentTransform = optionMenuParent.transform.FindChildRecursively("TabContent").transform as RectTransform;
            tabContentTransform.anchorMax = new Vector2(1f, 1f);

            var closeButtonTransform = optionMenuParent.transform.FindChildRecursively("CloseButton").transform as RectTransform;
            closeButtonTransform.anchorMin = new Vector2(1f, 1f);
            closeButtonTransform.anchorMax = new Vector2(1f, 1f);


            raftMMOTab.tab.transform.SetParentSafe(null);
            raftMMOTab.content.transform.SetParentSafe(null);
            Object.Destroy(raftMMOTab.tab);
            Object.Destroy(raftMMOTab.content);

            Traverse.Create(tabGroup).Field("tabButtons").SetValue(tabGroup.GetComponentsInChildren<TabButton>());

            raftMMOTab = null;
        }
    }
}
