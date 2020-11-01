using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace RaftMMO.Network
{
    public class RaftMMODeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return GetType(typeName);
        }

        public static Type GetType(string typeName)
        {
            return Type.GetType(string.Format("{0}, {1}", typeName, Assembly.GetExecutingAssembly().FullName));
        }
    }
}
