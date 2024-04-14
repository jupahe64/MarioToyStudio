using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Fushigi.Bfres.Common
{
    public class ResDict<T> : Dictionary<string, T>, IResData where T : IResData, new()
    {
        public ResDict() { }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Count)
                    return new T();

                return this[GetKey(index)];
            }
        }

        public string GetKey(int index)
        {
            if (index >= 0 && index < Keys.Count)
                return Keys.ElementAt(index);

            return null;
        }

        public int IndexOfKey(string key) => Keys.Select((k, i) => (k, i)).First(tup => tup.k == key).i;

        public void Read(BinaryReader reader)
        {
            reader.ReadUInt32(); //magic
            int numNodes = reader.ReadInt32();

            List<Node> nodes = new List<Node>();

            int i = 0;
            for (; numNodes >= 0; numNodes--)
            {
                nodes.Add(new Node()
                {
                    Reference = reader.ReadUInt32(),
                    IdxLeft = reader.ReadUInt16(),
                    IdxRight = reader.ReadUInt16(),
                    Key = reader.ReadStringOffset(reader.ReadUInt64()),
                });
                i++;
            }

            for (int j = 1; j < nodes.Count; j++)
                this.Add(nodes[j].Key, new T());

            //a lot of code depends on the Dictionary not reordering itself
            Debug.Assert(Keys.Select((key, i) => nodes[i+1].Key == key).All(x=>x));
        }

        protected class Node
        {
            internal uint Reference;
            internal ushort IdxLeft;
            internal ushort IdxRight;
            internal string Key;
            internal IResData Value;
        }
    }
}
