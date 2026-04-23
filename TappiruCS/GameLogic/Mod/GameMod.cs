using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TappiruCS.GameLogic.Mod
{
    [JsonDerivedType(typeof(NoFailMod), typeDiscriminator: "NoFail")]
    public abstract class GameMod 
    {
        public abstract string ModName { get; }
        public abstract float ScoreMultiply { get; }

        public abstract string ShortName { get; }

        public GameMod() { }

    }
}
