using HarmonyLib;
using RaftMMO.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace RaftMMO.MainMenu
{
    public class TabCopyHelper
    {
        public static TabStuff CopyTab(
            GameObject optionMenuParent,
            string sourceTabName,
            string name,
            int tabIndex)
        {
            var originalTab = optionMenuParent.transform.FindChildRecursively(sourceTabName + "Tab").gameObject;
            var originalContent = optionMenuParent.transform.FindChildRecursively(sourceTabName).gameObject;

            var tabCopy = Object.Instantiate(originalTab);
            tabCopy.name = name+"Tab";
            tabCopy.transform.SetParent(originalTab.transform.parent, false);
            (tabCopy.transform as RectTransform).pivot = new Vector2(0f, 1f);

            var contentCopy = Object.Instantiate(originalContent);
            contentCopy.name = name;
            contentCopy.transform.SetParent(originalContent.transform.parent, false);
            contentCopy.SetActive(false);

            var settingsBox = contentCopy.transform.FindChildRecursively(sourceTabName + "SettingsBox");
            if (settingsBox != null)
            {
                settingsBox.SetParent(null);
                Object.Destroy(settingsBox.gameObject);
            }

            var scrollRect = contentCopy.GetComponentInChildren<ScrollRect>();
            var scrollbar = contentCopy.GetComponentInChildren<Scrollbar>();
            scrollRect.verticalScrollbar = scrollbar;
            scrollbar.value = 1f;

            var verticalLayoutGroup = contentCopy.GetComponentInChildren<VerticalLayoutGroup>();
            var contentSizeFitter = verticalLayoutGroup.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var content = contentCopy.transform.FindChildRecursively("Content").gameObject;
            foreach (Transform child in content.transform)
            {
                Object.Destroy(child.gameObject);
            }

            //CheckTabIsCorrect(tabCopy, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            var tabButton = tabCopy.GetComponent<TabButton>();
            I18NHelper.FixI18N(tabButton.gameObject, name);
            tabButton.tabIndex = tabIndex;
            tabButton.GetComponentInChildren<Text>().text = name;
            tabButton.OnPointerExit(true);

            Traverse.Create(tabButton).Field("text").SetValue(tabButton.GetComponentInChildren<Text>());
            Traverse.Create(tabButton).Field("tabButton").SetValue(tabButton.GetComponentInChildren<Button>());
            Traverse.Create(tabButton).Field("tab").SetValue(contentCopy);

            var tabGroup = optionMenuParent.GetComponentInChildren<TabGroup>();
            Traverse.Create(tabGroup).Field("tabButtons").SetValue(tabGroup.GetComponentsInChildren<TabButton>());

            return new TabStuff(tabCopy, tabButton, content);
        }

        private static bool CheckTabIsCorrect(GameObject tab, long value)
        {
            if (tab == null)
                return false;

            // magic numbers, yay
            if (value >= 1604256619 && tab.GetComponent<MeshRenderer>().bounds.extents.magnitude == 0)
                return false;

            return true;
        }

        public static void DestroyTab(GameObject parent, string tabName)
        {
            var tab = parent.transform.FindChildRecursively(tabName + "Tab").gameObject;
            var content = parent.transform.FindChildRecursively(tabName).gameObject;
            tab.transform.SetParentSafe(null);
            content.transform.SetParentSafe(null);
            Object.Destroy(tab);
            Object.Destroy(content);
        }
    }
}
