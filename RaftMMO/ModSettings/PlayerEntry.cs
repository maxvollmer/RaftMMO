using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.ModSettings
{
    [System.Serializable()]
    public class PlayerEntry
    {
        public ulong steamID;

        public int model;
        public bool isBlocked;
        public long lastMet;
        public int metTimes;
        public int tradedTimes;
        public List<string> rafts = new List<string>();

        public PlayerEntry(ulong steamID, int model, long lastMet)
        {
            this.steamID = steamID;

            this.model = model;
            this.lastMet = lastMet;

            this.isBlocked = false;
            this.metTimes = 1;
            this.tradedTimes = 1;
        }

        public PlayerEntry() : this(0, 0, 0) { }
    }
}
