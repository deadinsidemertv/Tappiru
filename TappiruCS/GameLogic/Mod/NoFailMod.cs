using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TappiruCS.GameLogic.Mod
{
    public class NoFailMod : GameMod
    {
        public override string ModName => "NoFail";
        public override float ScoreMultiply => 0.5f;
        public override string ShortName => "NF";
        public NoFailMod(){} 
    }
}
