using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TappiruCS.Render.Text
{
    public class Font
    {
        private float _lineHeight;
        private float _scaleW;
        private float _scaleH;

        public float LineHeight => _lineHeight;
        public float TexWidth => _scaleW;
        public float TexHeight => _scaleH;

        public float CharWidth { get; private set; }
        public float CharHeight { get; private set; }

        public readonly Dictionary<int, int> PageTextures = new();
        public readonly Dictionary<char, GlyphInfo> _glyphs = new();
        public readonly Dictionary<(char first, char second), int> KerningPairs = new();

        public enum TextAlign { Left, Center, Right }

        public struct GlyphInfo
        {
            public int Page;
            public float TexX, TexY, Width, Height;
            public float XOffset, YOffset, XAdvance;
        }

        public Font(string fntPath)
        {
            LoadBMFont(fntPath);
            CalculateFallbackMetrics();
        }

        private void LoadBMFont(string fntPath)
        {
            if (!File.Exists(fntPath))
                throw new FileNotFoundException($"BMFont not found: {fntPath}");

            var lines = File.ReadAllLines(fntPath);
            var pageFiles = new Dictionary<int, string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.StartsWith("common")) ParseCommon(trimmed);
                else if (trimmed.StartsWith("page")) ParsePage(trimmed, pageFiles);
                else if (trimmed.StartsWith("char")) ParseChar(trimmed);
                else if (trimmed.StartsWith("kerning")) ParseKerning(trimmed);
            }

            foreach (var kvp in pageFiles)
            {
                string assetName = kvp.Value;
                assetName = Path.ChangeExtension(assetName, null);
                if (!assetName.Contains("/") && !assetName.Contains("\\"))
                    assetName = "Font/" + assetName;

                int texId = TextureManager.GetTexture(assetName);
                PageTextures[kvp.Key] = texId;
            }

            Console.WriteLine($"[BMFont] Loaded: {fntPath} | Pages: {PageTextures.Count} | Glyphs: {_glyphs.Count}");
        }

        private void CalculateFallbackMetrics()
        {
            if (_glyphs.Count == 0)
            {
                CharWidth = _lineHeight * 0.5f;
                CharHeight = _lineHeight;
                return;
            }

            float total = 0;
            foreach (var g in _glyphs.Values)
                total += g.XAdvance;

            CharWidth = total / _glyphs.Count;
            CharHeight = _lineHeight;
        }

        private void ParseCommon(string line)
        {
            var dict = ParseKeyValues(line);
            float.TryParse(dict.GetValueOrDefault("lineHeight", "0"), out _lineHeight);
            float.TryParse(dict.GetValueOrDefault("scaleW", "0"), out _scaleW);
            float.TryParse(dict.GetValueOrDefault("scaleH", "0"), out _scaleH);
        }

        private void ParsePage(string line, Dictionary<int, string> pageFiles)
        {
            var dict = ParseKeyValues(line);
            if (int.TryParse(dict.GetValueOrDefault("id", "-1"), out int id) && id >= 0)
            {
                string file = dict.GetValueOrDefault("file");
                if (!string.IsNullOrEmpty(file))
                    pageFiles[id] = file;
            }
        }

        private void ParseChar(string line)
        {
            var dict = ParseKeyValues(line);
            if (!int.TryParse(dict.GetValueOrDefault("id", "0"), out int id)) return;

            char c = (char)id;

            if (float.TryParse(dict.GetValueOrDefault("x", "0"), out float x) &&
                float.TryParse(dict.GetValueOrDefault("y", "0"), out float y) &&
                float.TryParse(dict.GetValueOrDefault("width", "0"), out float w) &&
                float.TryParse(dict.GetValueOrDefault("height", "0"), out float h) &&
                float.TryParse(dict.GetValueOrDefault("xoffset", "0"), out float xo) &&
                float.TryParse(dict.GetValueOrDefault("yoffset", "0"), out float yo) &&
                float.TryParse(dict.GetValueOrDefault("xadvance", "0"), out float xa))
            {
                int page = 0;
                int.TryParse(dict.GetValueOrDefault("page", "0"), out page);

                _glyphs[c] = new GlyphInfo
                {
                    Page = page,
                    TexX = x,
                    TexY = y,
                    Width = w,
                    Height = h,
                    XOffset = xo,
                    YOffset = yo,
                    XAdvance = xa
                };
            }
        }

        private void ParseKerning(string line)
        {
            var dict = ParseKeyValues(line);
            if (int.TryParse(dict.GetValueOrDefault("first", "0"), out int f) &&
                int.TryParse(dict.GetValueOrDefault("second", "0"), out int s) &&
                int.TryParse(dict.GetValueOrDefault("amount", "0"), out int amount))
            {
                KerningPairs[((char)f, (char)s)] = amount;
            }
        }

        private Dictionary<string, string> ParseKeyValues(string line)
        {
            var dict = new Dictionary<string, string>();
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=');
                if (kv.Length == 2)
                    dict[kv[0]] = kv[1].Trim('"');
            }
            return dict;
        }

        // ========== Public API ==========

        public float GetScaleFromFontSize(float fontSize) => fontSize / _lineHeight;

        public bool TryGetGlyph(char c, out GlyphInfo glyph) => _glyphs.TryGetValue(c, out glyph);

        public int GetKerning(char first, char second) =>
            KerningPairs.TryGetValue((first, second), out int k) ? k : 0;

        public int GetTextureForPage(int page) =>
            PageTextures.TryGetValue(page, out int id) ? id : PageTextures.Values.FirstOrDefault();

        public (float u1, float v1, float u2, float v2) GetUV(in GlyphInfo g) =>
            (g.TexX / TexWidth, g.TexY / TexHeight,
             (g.TexX + g.Width) / TexWidth, (g.TexY + g.Height) / TexHeight);

        public float CalculateTextWidth(string text, float scale)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float total = 0f;
            char prev = '\0';
            foreach (char c in text)
            {
                if (_glyphs.TryGetValue(c, out var g))
                {
                    total += (g.XAdvance + GetKerning(prev, c)) * scale;
                    prev = c;
                }
                else
                {
                    total += CharWidth * scale;
                    prev = c;
                }
            }
            return total;
        }

        public Vector2 MeasureString(string text, float scaleX, float scaleY)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;
            return new Vector2(CalculateTextWidth(text, scaleX), _lineHeight * scaleY);
        }
    }
}