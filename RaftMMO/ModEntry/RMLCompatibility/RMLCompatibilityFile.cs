
// This is a compatibility file.
// This file will be compiled into the RaftMMO.dll for the Unity Mod Loader by newman55.
// This file will be ignored when packing the rmod archive for the Raft Mod Loader by TeKGamer.
// It simply exists to make the code compile for UMM without referencing the RML dll.
// It will not be used when running the mod with UMM, because UMM will load the mod through UMMModEntry, not RMLModEntry.
// It will not be used when running the mod with RML, because RML will provide the correct classes.

using UnityEngine;

public class Mod
{
    protected readonly GameObject gameObject = null;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "See comment above.")]
    public byte[] GetEmbeddedFileBytes(string path) { return null; }
}

namespace Newtonsoft.Json
{
    public class JsonConvert
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "See comment above.")]
        public static T DeserializeObject<T>(string json) { return default; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "See comment above.")]
        public static string SerializeObject(object o, object indented) { return null; }
    }

    public enum Formatting
    {
        None,
        Indented
    }
}
