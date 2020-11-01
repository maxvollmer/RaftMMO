using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftMMO.ModEntry
{
    public interface IModJsonLib
    {
        T Deserialize<T>(string json);
        string Serialize(object o);
    }
}
