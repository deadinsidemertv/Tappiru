using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Render.Text.FreeType;

namespace TappiruCS.Render.Text
{
    public static class FontManager
    {
        public static FreeTypeRender CurrentFont { get; set; }

        // Дополнительные шрифты можно хранить здесь же (словарь)
        // public static Dictionary<string, FreeTypeFont> Fonts { get; }

        public static void DisposeCurrent()
        {
            CurrentFont?.Dispose();
            CurrentFont = null;
        }
    }
}
