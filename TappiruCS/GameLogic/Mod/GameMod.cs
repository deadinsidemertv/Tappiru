using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.GameLogic.Mod
{
    public abstract class GameMod
    {
        public string ModName { get; set; }
        public float ScoreMultiply { get; set; }

        public string ShortName { get; set; }
    }
}
