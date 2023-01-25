namespace RaftMMO.ModEntry
{
    public interface IModJsonLib
    {
        T Deserialize<T>(string json);
        string Serialize(object o);
    }
}
