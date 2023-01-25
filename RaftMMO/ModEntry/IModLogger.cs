namespace RaftMMO.ModEntry
{
    public interface IModLogger
    {
        void LogError(string message);
        void LogWarning(string message);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogAlways(string message);
    }
}
