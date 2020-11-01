using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.Network.Messages
{
    public interface IPositionUpdateMessage
    {
        SerializableData.Vector Position { get; }
        SerializableData.Angles Rotation { get; }
        float RemotePosRotation { get; }
    }
}
