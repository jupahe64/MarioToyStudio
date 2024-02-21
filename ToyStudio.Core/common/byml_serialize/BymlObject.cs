using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.common.byml_serialize
{
    public class BymlObject
    {
        public BymlMap BymlMap;

        public void Load(BymlMap bymlMap) {
            BymlMap = bymlMap;
            Deserialize();
        }

        public void Deserialize()
        {
            BymlSerialize.Deserialize(this, BymlMap);
        }

        public Byml Serialize()
        {
            BymlMap hashTable = BymlSerialize.Serialize(this);
            //Merge hash tables. Keep original params intact
            foreach (var pair in hashTable)
            {
                //Update or add any hash table params
                BymlMap[pair.Key] = pair.Value;
            }
            return hashTable;
        }
    }
}
