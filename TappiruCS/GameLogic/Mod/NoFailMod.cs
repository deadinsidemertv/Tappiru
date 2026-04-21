using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.GameLogic.Mod
{
    public class NoFailMod : GameMod
    {
        public NoFailMod()
        {
            ModName = "NoFail";
            ScoreMultiply = 0.5f;
        } 
    }
}
