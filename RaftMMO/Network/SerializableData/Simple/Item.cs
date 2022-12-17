using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.Network.SerializableData
{
    [Serializable()]
    public class Item
    {
        public int uniqueIndex;
        public int amount;
        public int uses;

        public Item(int uniqueIndex, int amount, int uses)
        {
            this.uniqueIndex = uniqueIndex;
            this.amount = amount;
            this.uses = uses;
        }

        // for serialization
        public Item() { }
    }
}
