﻿using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.common.byml_serialization
{
    public abstract class BymlObject<T> : IBymlObject<T>
         where T : BymlObject<T>, new()
    {
        private BymlMap? _map;

        public static T Deserialize(Byml byml)
        {
            var obj = new T
            {
                _map = byml.GetMap()
            };
            obj.Deserialize(new Deserializer(byml.GetMap()));
            return obj;
        }

        protected abstract void Deserialize(Deserializer d);

        public Byml Serialize()
        {
            _map ??= [];

            Serialize(new Serializer(_map));
            return new Byml(_map!);
        }

        public static Byml Serialize(T obj) => obj.Serialize();

        protected abstract void Serialize(Serializer s);



        public static List<TItem> ParseArray<TItem>(Byml byml, Func<Byml, TItem> mapper)
        {
            var bymlArray = byml.GetArray();
            var list = new List<TItem>();

            foreach (var item in bymlArray)
            {
                list.Add(mapper(item));
            }

            return list;
        }

        public static Dictionary<string, TItem> ParseMap<TItem>(Byml byml, Func<Byml, TItem> mapper)
        {
            var bymlMap = byml.GetMap();
            var dict = new Dictionary<string, TItem>();

            foreach (var (key, value) in bymlMap)
            {
                dict.Add(key, mapper(value));
            }

            return dict;
        }

        public static int ParseInt32(Byml byml) => byml.GetInt();
        public static uint ParseUInt32(Byml byml) => byml.GetUInt32();
        public static long ParseInt64(Byml byml) => byml.GetInt64();
        public static ulong ParseUInt64(Byml byml) => byml.GetUInt64();
        public static float ParseFloat(Byml byml) => byml.GetFloat();
        public static double ParseDouble(Byml byml) => byml.GetDouble();
        public static string ParseString(Byml byml) => byml.GetString();
        public static bool ParseBool(Byml byml) => byml.GetBool();

        

        public class Deserializer(BymlMap map)
        {
            public BymlMap Map => map;

            #region generated code

            //generated by this code (with the code above as input)

            /*
            var regex = new System.Text.RegularExpressions.Regex("\\s*public static (.*) Parse(.*?)\\(Byml byml(.*?)\\).*");

            foreach (string line in input.Split('\n')) {
                var m = regex.Match(line);
                if (!m.Success)
                    continue;

                var valueType = m.Groups[1].Value;
                var suffix = m.Groups[2].Value;
                var mapperParameter = m.Groups[3].Value;

                Console.WriteLine($$"""
                        public void Set{{suffix}}(ref {{valueType}} value, string name{{mapperParameter}})
                        {
                            if (!map!.TryGetValue(name, out var node))
                                return;

                            value = Parse{{suffix}}(node{{(mapperParameter == "" ? "" : $", {mapperParameter.Split(' ')[^1]}")}});
                        }
            """);
            }
            */

            public void SetArray<TItem>(ref List<TItem> value, string name, Func<Byml, TItem> mapper)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseArray<TItem>(node, mapper);
            }
            public void SetMap<TItem>(ref Dictionary<string, TItem> value, string name, Func<Byml, TItem> mapper)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseMap<TItem>(node, mapper);
            }
            public void SetInt32(ref int value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseInt32(node);
            }
            public void SetUInt32(ref uint value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseUInt32(node);
            }
            public void SetInt64(ref long value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseInt64(node);
            }
            public void SetUInt64(ref ulong value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseUInt64(node);
            }
            public void SetFloat(ref float value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseFloat(node);
            }
            public void SetDouble(ref double value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseDouble(node);
            }
            public void SetString(ref string value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseString(node);
            }
            public void SetBool(ref bool value, string name)
            {
                if (!map!.TryGetValue(name, out var node))
                    return;

                value = ParseBool(node);
            }

            #endregion
        }

        public static Byml SerializeArray<TItem>(List<TItem> list, Func<TItem, Byml> mapper)
        {
            var bymlArray = new BymlArray();

            foreach (var item in list)
            {
                bymlArray.Add(mapper(item));
            }

            return new Byml(bymlArray);
        }

        public static Byml SerializeMap<TItem>(Dictionary<string, TItem> dict, Func<TItem, Byml> mapper)
        {
            var bymlMap = new BymlMap();

            foreach (var (key, value) in dict)
            {
                bymlMap.Add(key, mapper(value));
            }

            return new Byml(bymlMap);
        }

        public static Byml SerializeInt32(int value) => new Byml(value);
        public static Byml SerializeUInt32(uint value) => new Byml(value);
        public static Byml SerializeInt64(long value) => new Byml(value);
        public static Byml SerializeUInt64(ulong value) => new Byml(value);
        public static Byml SerializeFloat(float value) => new Byml(value);
        public static Byml SerializeDouble(double value) => new Byml(value);
        public static Byml SerializeString(string value) => new Byml(value);
        public static Byml SerializeBool(bool value) => new Byml(value);

        public class Serializer(BymlMap map)
        {
            public BymlMap Map => map;

            #region generated code

            //generated by this code (with the code above as input)

            /*
            var regex = new System.Text.RegularExpressions.Regex(
                "\\s*public static Byml Serialize(.*?)\\(([a-zA-Z]*(?:<.*?>)?) [a-zA-Z]*?(,.*?)?\\).*");

            foreach (string line in input.Split('\n')) {
                var m = regex.Match(line);
                if (!m.Success)
                    continue;

                var suffix = m.Groups[1].Value;
                var valueType = m.Groups[2].Value;
                var mapperParameter = m.Groups[3].Value;

                Console.WriteLine($$"""
                        public void Set{{suffix}}(ref {{valueType}} value, string name{{mapperParameter}})
                        {
                            map[name] = Serialize{{suffix}}(value{{(mapperParameter == "" ? "" : $", {mapperParameter.Split(' ')[^1]}")}});
                        }
            """);
            }
            */
            public void SetArray<TItem>(ref List<TItem> value, string name, Func<TItem, Byml> mapper)
            {
                map[name] = SerializeArray<TItem>(value, mapper);
            }
            public void SetMap<TItem>(ref Dictionary<string, TItem> value, string name, Func<TItem, Byml> mapper)
            {
                map[name] = SerializeMap<TItem>(value, mapper);
            }
            public void SetInt32(ref int value, string name)
            {
                map[name] = SerializeInt32(value);
            }
            public void SetUInt32(ref uint value, string name)
            {
                map[name] = SerializeUInt32(value);
            }
            public void SetInt64(ref long value, string name)
            {
                map[name] = SerializeInt64(value);
            }
            public void SetUInt64(ref ulong value, string name)
            {
                map[name] = SerializeUInt64(value);
            }
            public void SetFloat(ref float value, string name)
            {
                map[name] = SerializeFloat(value);
            }
            public void SetDouble(ref double value, string name)
            {
                map[name] = SerializeDouble(value);
            }
            public void SetString(ref string value, string name)
            {
                map[name] = SerializeString(value);
            }
            public void SetBool(ref bool value, string name)
            {
                map[name] = SerializeBool(value);
            }
            #endregion
        }
    }

    public interface IBymlObject<T> where T : new()
    {
        public static abstract T Deserialize(Byml byml);

        public Byml Serialize();
    }

    public interface IBymlObject
    {
        public Byml Serialize();
    }
}
