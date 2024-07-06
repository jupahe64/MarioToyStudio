using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using ToyStudio.Core.Level.Objects;

namespace ToyStudio.Core.Level
{
    public class SubLevel(Level level, string bcettName, string levelParamName, string lightingParamName)
    {
        public string BcettName = bcettName;
        public string LevelParamName = levelParamName;
        public string LightingParamName = lightingParamName;

        public List<LevelActor> Actors = [];
        public List<LevelAiGroup> AiGroups = [];
        public List<LevelRail> Rails = [];
        public List<KeyValuePair<string, Byml>> OtherBymlEntries = [];

        public void LoadFromBcett(BymlMap bcett)
        {
            var bymlEntries = bcett.ToList();

            if (bcett.TryGetValue("Actors", out var node))
            {
                var actors = node.GetArray();
                Actors.AddRange(actors.Select(LevelActor.Deserialize));
            }
            if (bcett.TryGetValue("AiGroups", out node))
            {
                var aiGroups = node.GetArray();
                AiGroups.AddRange(aiGroups.Select(LevelAiGroup.Deserialize));
            }
            if (bcett.TryGetValue("Rails", out node))
            {
                var rails = node.GetArray();
                Rails.AddRange(rails.Select(LevelRail.Deserialize));
            }

            OtherBymlEntries = bcett.Where(x=>x.Key is not ("Actors" or "AiGroups" or "Rails")).ToList();
        }

        public Byml Save()
        {
            var map = new BymlMap();

            var entries = new List<KeyValuePair<string, Byml>>();

            if (Actors.Count > 0)
            {
                var actors = new BymlArray();
                actors.AddRange(Actors.Select(LevelActor.Serialize));
                entries.Add(new("Actors", actors));
            }

            if (AiGroups.Count > 0)
            {
                var aiGroups = new BymlArray();
                aiGroups.AddRange(AiGroups.Select(LevelAiGroup.Serialize));
                entries.Add(new("AiGroups", aiGroups));
            }

            if (Rails.Count > 0)
            {
                var rails = new BymlArray();
                rails.AddRange(Rails.Select(LevelRail.Serialize));
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
