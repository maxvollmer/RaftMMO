
using System;

namespace RaftMMO.ModSettings
{
    [System.Serializable()]
    public class RaftEntry
    {
        public ulong steamID;
        public string sessionID;

        public int numPlayers;
        public bool isFavorite;
        public bool isBlocked;
        public long lastMet;
        public int metTimes;

        public RaftEntry(ulong steamID, string sessionID, int numPlayers, long lastMet)
        {
            this.steamID = steamID;
            this.sessionID = sessionID;

            this.numPlayers = numPlayers;
            this.lastMet = lastMet;

            this.isFavorite = false;
            this.isBlocked = false;
            this.metTimes = 1;
        }

        public RaftEntry() : this(0, "", 0, 0) {}
    }
}
