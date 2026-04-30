using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Render.Text.FreeType;

namespace TappiruCS.Render.Text
{
    public static class FontManager
    {
        public static FreeTypeRender? _defaultFont;

        public static void SetDefault(string key)
        {
            if (_fonts.TryGetValue(key, out var font))
                _defaultFont = font;
            else
                Console.WriteLine($"[FontManager] Ключ '{key}' не найден — дефолтный шрифт не установлен!");
        }

        private static readonly Dictionary<string, FreeTypeRender> _fonts = new();

        public static void Add(string key, FreeTypeRender renderer)
        => _fonts[key] = renderer;

        public static FreeTypeRender? Get(string key)
        {
            if (_fonts.TryGetValue(key, out var font))
                return font;

            // fallback: если нет дефолтного, пытаемся взять любой первый загруженный
            if (_defaultFont != null)
                return _defaultFont;

            // если совсем ничего нет – возвращаем null (но этого почти не случится, если дефолтный задан)
            return null;
        }

        public static void DisposeAll()
        {
            foreach (var font in _fonts.Values)
                font.Dispose();
            _fonts.Clear();
        }
    }
}
