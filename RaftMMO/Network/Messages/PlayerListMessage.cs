using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.Network.Messages
{
    [System.Serializable()]
    public class PlayerListMessage : BaseMessage
    {
        public readonly ulong[] players;

        public PlayerListMessage(ulong[] players)
            : base(MessageType.LIST_OF_PLAYERS, true)
        {
            this.players = players;
        }
    }
}
