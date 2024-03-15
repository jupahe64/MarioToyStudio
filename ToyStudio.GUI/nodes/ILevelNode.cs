using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.GUI.util.edit;

namespace ToyStudio.GUI.nodes
{
    internal interface ILevelNode
    {
        void Update(LevelNodeTreeUpdater updater, LevelNodeContext nodeContext, ref bool isValid);
    }
}
