
using Newtonsoft.Json;
using RaftMMO.Utilities;
using UnityEngine;

namespace RaftMMO.ModEntry
{
    public class RMLModEntry : Mod, IModDataGetter
    {
        private class ModJsonLib : IModJsonLib
        {
            public T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }

            public string Serialize(object o)
            {
                return JsonConvert.SerializeObject(o, Formatting.Indented);
            }
        }

        private class RMLLogger : IModLogger
        {
            public void LogError(string message)
            {
                Debug.LogError(message);
            }

            public void LogWarning(string message)
            {
                Debug.LogWarning(message);
            }

            public void LogDebug(string message)
            {
                Debug.Log(message);
            }

            public void LogInfo(string message)
            {
                Debug.Log(message);
            }

            public void LogAlways(string message)
            {
                Debug.Log(message);
            }
        }

        public void Start()
        {
            RaftMMOLogger.ModLogger = new RMLLogger();
            CommonEntry.OnModLoad(this, new ModJsonLib());
        }

        public void OnModUnload()
        {
            Object.Destroy(gameObject);
            CommonEntry.OnModUnload();
        }

        public void Update()
        {
            CommonEntry.Update();
        }

        public byte[] GetDataFile(string name)
        {
            return GetEmbeddedFileBytes(name);
        }

        public byte[] GetModFile(string name)
        {
            return GetEmbeddedFileBytes(name);
        }
    }
}
