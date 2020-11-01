using RaftMMO.Network.SerializableData;
using UnityEngine;

namespace RaftMMO.Utilities
{
    public class GameObjectDebugger
    {
        private static string Print(GameObject gameObject)
        {
            return gameObject.ToString() + " (" + gameObject.activeSelf + ")";
        }

        private static string Print(Component component)
        {
            return component.ToString();
        }

        private static string PrintParents(GameObject o)
        {
            if (o == null)
                return "null";

            return Print(o) + " < " + PrintParents(o.transform.parent?.gameObject);
        }

        private static string CreateObjectDebugString(GameObject o, bool includeComponents, int level)
        {
            string indention = new string(' ', level * 2);
            string bla = "";

            if (level == 0)
            {
                bla += indention + PrintParents(o) + "\n";
            }
            else
            {
                bla += indention + Print(o) + "\n";
            }

            if (includeComponents)
            {
                foreach (var component in o.GetComponents<Component>())
                {
                    bla += indention + "- " + Print(component) + "\n";
                }
            }

            foreach (Transform child in o.transform)
            {
                bla += CreateObjectDebugString(child.gameObject, includeComponents, level + 1);
            }

            return bla;
        }

        public static void DebugPrint(RectTransform transform, string name="transform")
        {
            RaftMMOLogger.LogDebug(
                name + ".position: " + transform.position + "\n"
                + name + ".localPosition: " + transform.localPosition + "\n"
                + name + ".anchorMin: " + transform.anchorMin + "\n"
                + name + ".anchorMax: " + transform.anchorMax + "\n"
                + name + ".sizeDelta: " + transform.sizeDelta + "\n"
                + name + ".pivot: " + transform.pivot + "\n"
                + name + ".localScale: " + transform.localScale + "\n"
                + name + ".lossyScale: " + transform.lossyScale + "\n"
                + name + ".rect: " + transform.rect + "\n");
        }

        public static void DebugPrint(GameObject o, bool includeComponents = true)
        {
            RaftMMOLogger.LogDebug(CreateObjectDebugString(o, includeComponents, 0));
        }

        public static string DebugPrint(RaftBlockData[] blocks)
        {
            return "[" + string.Join<RaftBlockData>(",", blocks) + "]";
        }

        public static string DebugPrint(RaftColliderData[] colliders)
        {
            return "[" + string.Join<RaftColliderData>(",", colliders) + "]";
        }

        public static string DebugPrint(RaftPlantData[] plants)
        {
            return "[" + string.Join<RaftPlantData>(",", plants) + "]";
        }
    }
}
