
using UnityEngine;

namespace RaftMMO.Utilities
{
    public class I18NHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Implementation unfinished. See comment in code.")]
        public static void FixI18N(GameObject gameObject, string text)
        {
            // TODO: For now we just delete these, so the game doesn't override our text.
            // In future we want to add our own, so RaftMMO menu items have proper language support.
            foreach (var localize in gameObject.GetComponentsInChildren<I2.Loc.Localize>())
            {
                localize.enabled = false;
                Object.Destroy(localize);
            }
        }
    }
}
