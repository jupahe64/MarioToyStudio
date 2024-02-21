using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ToyStudio.Core.common.byml_serialize
{
    public static class BymlSerialize
    {
        public static T Deserialize<T>(Span<byte> data)
        {
            return Deserialize<T>(Byml.FromBinary(data));
        }

        public static T Deserialize<T>(Byml node)
        {
            T instance = (T)CreateInstance(typeof(T));
            Deserialize(instance, node);
            return instance;
        }

        public static void Deserialize(object obj, Byml node)
        {
            var hashTable = node.GetMap();

            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                //Only load properties with byaml attributes
                var byamlAttribute = properties[i].GetCustomAttribute<BymlProperty>();
                var bymlIgnoreAttribute = properties[i].GetCustomAttribute<BymlIgnore>();

                if (bymlIgnoreAttribute != null)
                    continue;

                Type type = properties[i].PropertyType;
                Type? nullableType = Nullable.GetUnderlyingType(type);
                if (nullableType != null)
                    type = nullableType;

                //Set custom keys as property name if used
                string name = byamlAttribute != null && byamlAttribute.Key != null ? byamlAttribute.Key : properties[i].Name;

                //Skip properties that are not present
                if (!hashTable.ContainsKey(name))
                    continue;

                //SetValues(properties[i], type, obj, hashTable[name]);
            }
        }

        public static BymlMap Serialize(object target)
        {
            var bymlProperties = new BymlMap();

            var properties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                //Only load properties with byaml attributes
                var byamlAttribute = properties[i].GetCustomAttribute<BymlProperty>();
                var bymlIgnoreAttribute = properties[i].GetCustomAttribute<BymlIgnore>();

                if (bymlIgnoreAttribute != null)
                    continue;

                var value = properties[i].GetValue(target);

                if (byamlAttribute != null)
                    value = byamlAttribute.DefaultValue;

                //Set custom keys as property name if used
                string name = byamlAttribute != null && byamlAttribute.Key != null ? byamlAttribute.Key : properties[i].Name;

                if (value == null)
                    continue;

                var node = Serialize(value);
                //bymlProperties.AddNode(node.Id, node, name);
            }
            return bymlProperties;
        }




        static bool IsTypeSerializableObject(Type type)
        {
            return Attribute.IsDefined(type, typeof(SerializableAttribute));
        }


        private static bool IsTypeList(Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IList));
        }

        private static T InstantiateType<T>(Type type)
        {
            // Validate if the given type is compatible with the required one.
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type))
            {
                throw new Exception($"Type {type.Name} cannot be used as BYAML object data.");
            }
            // Return a new instance.
            return (T)CreateInstance(type);
        }

        static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, true);
        }
    }
}
