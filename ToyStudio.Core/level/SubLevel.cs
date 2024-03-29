﻿using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.level
{
    public class SubLevel(Level level, string bcettName, string levelParamName, string lightingParamName)
    {
        public string BcettName = bcettName;
        public string LevelParamName = levelParamName;
        public string LightingParamName = lightingParamName;

        public List<LevelActor> Actors = [];
        public List<LevelRail> Rails = [];
        public List<KeyValuePair<string, Byml>> OtherBymlEntries = [];

        public void LoadFromBcett(BymlMap bcett)
        {
            if (bcett.TryGetValue("Actors", out var node))
            {
                var actors = node.GetArray();

                foreach (var actorNode in actors )
                {
                    var actor = LevelActor.Deserialize(actorNode);
                    Actors.Add(actor);
                }
            }
            if (bcett.TryGetValue("Rails", out node))
            {
                var rails = node.GetArray();

                foreach (var railNode in rails)
                {
                    var rail = LevelRail.Deserialize(railNode);
                    Rails.Add(rail);
                }
            }

            OtherBymlEntries = bcett.Where(x=>x.Key is not ("Actors" or "Rails")).ToList();
        }

        public Byml Save()
        {
            var map = new BymlMap();

            var entries = new List<KeyValuePair<string, Byml>>();

            if (Actors.Count > 0)
            {
                var actors = new BymlArray();
                foreach (var actor in Actors)
                    actors.Add(actor.Serialize());
                entries.Add(new("Actors", actors));
            }

            if (Rails.Count > 0)
            {
                var rails = new BymlArray();
                foreach (var rail in Rails)
                    rails.Add(rail.Serialize());
                entries.Add(new("Rails", rails));
            }

            entries.AddRange(OtherBymlEntries);

            //temporary solution
            entries.Sort((l,r)=>l.Key.CompareTo(r.Key));
            map.EnsureCapacity(entries.Count);
            foreach (var entry in entries)
                map[entry.Key] = entry.Value;

            return new Byml(map);
        }
    }
}
