using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.UI.API
{
    public static class ContentPath
    {
        public const string TEXTURES_ROOT = "Content/Textures";
        public const string CONTENT_TEXTURES_FONT = "Content\\Textures\\Font";
        public const string CONTENT_TEXTURES_BACKGROUNDS = "Content\\Textures\\Backgrounds";
        public const string CONTENT_TEXTURES_SMALLGRADE = "Content\\Textures\\SmallGrade";
        public const string CONTENT_SOUND_SFX = "Content\\Sound\\SFX";





        public static string Combine(this string path1, string path2) => System.IO.Path.Combine(path1, path2);
    }


}
