using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core.common.byml_serialization;
using ToyStudio.GUI.common.util;
using YamlDotNet.Core.Tokens;

namespace ToyStudio.Core.common.util
{
    /// <summary>
    /// Parse, Serialize and Set functions specifically for module system games
    /// </summary>
    internal static class BymlObjectExtensions
    {
        #region Float3
        public static Vector3 ParseFloat3(Byml node)
        {
            var array = node.GetArray();
            return new Vector3(
                array[0].GetFloat(),
                array[1].GetFloat(),
                array[2].GetFloat()
                );
        }

        public static void SetFloat3<T>(this BymlObject<T>.Deserializer s,
            ref Vector3 value, string name)
            where T : BymlObject<T>, new()
        {
            if (!s.Map!.TryGetValue(name, out var node))
                return;

            value = ParseFloat3(node);
        }

        public static Byml SerializeFloat3(Vector3 value)
        {
            return new Byml(new BymlArray()
            {
                new Byml(value.X),
                new Byml(value.Y),
                new Byml(value.Z)
            });
        }

        public static void SetFloat3<T>(this BymlObject<T>.Serializer s,
            ref Vector3 value, string name, Vector3? defaultValue = null)
            where T : BymlObject<T>, new()
        {
            if (value == defaultValue)
                s.Map.Remove(name);
            else
                s.Map[name] = SerializeFloat3(value);
        }
        #endregion

        #region Vector3
        public static Vector3 ParseVector3D(Byml node)
        {
            var map = node.GetMap();
            return new Vector3(
                map["X"].GetFloat(),
                map["Y"].GetFloat(),
                map["Z"].GetFloat()
                );
        }

        public static void SetVector3D<T>(this BymlObject<T>.Deserializer s,
            ref Vector3 value, string name)
            where T : BymlObject<T>, new()
        {
            if (!s.Map!.TryGetValue(name, out var node))
                return;

            value = ParseVector3D(node);
        }

        public static Byml SerializeVector3D(Vector3 value)
        {
            return new Byml(new BymlMap()
            {
                ["X"] = new Byml(value.X),
                ["Y"] = new Byml(value.Y),
                ["Z"] = new Byml(value.Z)
            });
        }

        public static void SetVector3D<T>(this BymlObject<T>.Serializer s,
            ref Vector3 value, string name, Vector3? defaultValue = null)
            where T : BymlObject<T>, new()
        {
            if (value == defaultValue)
                s.Map.Remove(name);
            else
                s.Map[name] = SerializeVector3D(value);
        }
        #endregion

        #region PropertyDict
        public static PropertyDict ParsePropertyDict(Byml node)
        {
            var map = node.GetMap();

            List<PropertyDict.Entry> entries = [];
            foreach (var (key, value) in map)
            {
                object parsed = value.Type switch
                {
                    BymlNodeType.String => value.GetString(),
                    BymlNodeType.Bool => value.GetBool(),
                    BymlNodeType.Int => value.GetInt(),
                    BymlNodeType.UInt32 => value.GetUInt32(),
                    BymlNodeType.Int64 => value.GetInt64(),
                    BymlNodeType.UInt64 => value.GetUInt64(),
                    BymlNodeType.Float => value.GetFloat(),
                    BymlNodeType.Double => value.GetDouble(),
                    BymlNodeType.Array => ParseFloat3(value),
                    BymlNodeType.Null => null!,
                    _ => throw new Exception($"Unexpected node type {value.Type}"),
                };

                entries.Add(new(key, parsed));
            }

            return new PropertyDict(entries);
        }

        public static void SetPropertyDict<T>(this BymlObject<T>.Deserializer s,
            ref PropertyDict value, string name)
            where T : BymlObject<T>, new()
        {
            if (!s.Map!.TryGetValue(name, out var node))
                return;

            value = ParsePropertyDict(node);
        }

        public static Byml SerializePropertyDict(PropertyDict dict)
        {
            var map = new BymlMap();

            foreach (var entry in dict)
            {
                Byml serialized = entry.Value switch
                {
                    string v => new Byml(v),
                    bool v => new Byml(v),
                    int v => new Byml(v),
                    uint v => new Byml(v),
                    long v => new Byml(v),
                    ulong v => new Byml(v),
                    float v => new Byml(v),
                    double v => new Byml(v),
                    Vector3 v => SerializeFloat3(v),
                    null => new Byml(),
                    _ => throw new Exception($"Unexpected object type {entry.Value.GetType()}"),
                };

                map.Add(entry.Key, serialized);
            }

            return new Byml(map);
        }

        public static void SetPropertyDict<T>(this BymlObject<T>.Serializer s,
            ref PropertyDict value, string name)
            where T : BymlObject<T>, new()
        {
            if (value.Count > 0)
                s.Map[name] = SerializePropertyDict(value);
            else
                s.Map.Remove(name);
        }

        #endregion
    }
}
