using BymlLibrary;
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
        }
    }
}
