
namespace RaftMMO.ModEntry
{
    public interface IModDataGetter
    {
        byte[] GetDataFile(string name);
        byte[] GetModFile(string name);
    }
}
