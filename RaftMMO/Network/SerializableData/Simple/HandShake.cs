using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.Network.SerializableData.Simple
{
    [System.Serializable()]
    public struct HandShake
    {
        public readonly int mine;
        public readonly int theirs;

        public HandShake(int mine, int theirs)
        {
            this.mine = mine;
            this.theirs = theirs;
        }
    }
}
