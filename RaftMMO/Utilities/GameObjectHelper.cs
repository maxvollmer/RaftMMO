using System.Collections.Generic;
using UnityEngine;

namespace RaftMMO.Utilities
{
    public static class GameObjectHelper
    {
        public static void DestroyAllChildren(this GameObject parent)
        {
            var children = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                child.transform.SetParent(null);
                Object.Destroy(child);
            }
        }
    }
}
