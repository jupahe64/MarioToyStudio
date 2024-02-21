using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core
{
    public class Level
    {
        public static Level Load(string sceneName, RomFS romfs)
        {
            var level = new Level(sceneName, romfs);

            //TODO

            return level;
        }

        private Level(string sceneName, RomFS romfs) 
        { 
            _sceneName = sceneName;
            _romfs = romfs;
        }

        private string _sceneName;
        private RomFS _romfs;
    }
}
