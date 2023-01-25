using UnityEngine;

namespace RaftMMO.MainMenu
{
    public class TabStuff
    {
        public readonly GameObject tab;
        public readonly TabButton tabButton;

        public readonly GameObject content;

        public TabStuff(GameObject tab, TabButton tabButton, GameObject content)
        {
            this.tab = tab;
            this.tabButton = tabButton;
            this.content = content;
        }
    }
}
