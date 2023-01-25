namespace RaftMMO.Network.Messages
{
    public interface IPositionUpdateMessage
    {
        SerializableData.Vector Position { get; }
        SerializableData.Angles Rotation { get; }
        float RemotePosRotation { get; }
    }
}
