using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace TappiruCS.Render
{
    public class TextRender
    {
        private readonly SpriteBatch _spriteBatch;

        public readonly Dictionary<int, int> _pageTextures = new();
        private readonly Dictionary<char, GlyphInfo> _glyphs = new();
        public readonly Dictionary<(char first, char second), int> _kerningPairs = new();

        public float _lineHeight;
        public float _scaleW;
        public float _scaleH;

        // Для совместимости со старым кодом
        public float charWidth, charHeight;
        public float texWidth, texHeight;

        public enum TextAlign { Left, Center, Right }

        public struct GlyphInfo
        {
            public int Page;
            public float TexX, TexY, Width, Height;
            public float XOffset, YOffset, XAdvance;
        }

        public TextRender(SpriteBatch spriteBatch, string fntPath)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            LoadBMFont(fntPath);
        }

        private void LoadBMFont(string fntPath)
        {
            if (!File.Exists(fntPath))
            {
                Console.WriteLine($"[ERROR] BMFont не найден: {fntPath}");
                return;
            }

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
                _pageTextures[kvp.Key] = texId;
            }

            texWidth = _scaleW;
            texHeight = _scaleH;
            charWidth = texWidth / 20f;
            charHeight = _lineHeight;

            Console.WriteLine($"[BMFont] УСПЕШНО ЗАГРУЖЕН: {fntPath} | Страниц: {_pageTextures.Count} | Глифов: {_glyphs.Count}");
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
                _kerningPairs[((char)f, (char)s)] = amount;
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

        private (float u1, float v1, float u2, float v2) GetUV(in GlyphInfo g)
        {
            return (g.TexX / texWidth, g.TexY / texHeight,
                    (g.TexX + g.Width) / texWidth, (g.TexY + g.Height) / texHeight);
        }

        private int GetTextureForPage(int page)
        {
            return _pageTextures.TryGetValue(page, out int id) ? id : _pageTextures.Values.FirstOrDefault();
        }

        private float GetKerning(char first, char second)
        {
            return _kerningPairs.TryGetValue((first, second), out int k) ? k : 0f;
        }

        // ====================== ОСНОВНЫЕ МЕТОДЫ ======================

        public void DrawString(string text, float x, float y, float scaleX, float scaleY,float r, float g, float b, float a, Matrix4 projection,TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            float startX = CalculateStartX(text, scaleX, x, align);
            DrawStringInternal(text, startX, y, scaleX, scaleY, r, g, b, a, projection);
        }

        public void DrawString(string text, float x, float y, float scale,float r, float g, float b, float a, Matrix4 projection,TextAlign align = TextAlign.Left)
        {
            DrawString(text, x, y, scale, scale, r, g, b, a, projection, align);
        }

        private void DrawStringInternal(string text, float startX, float startY,float scaleX, float scaleY,float r, float g, float b, float a, Matrix4 projection)
        {
            float currentX = startX;
            char prevChar = '\0';

            foreach (char c in text)
            {
                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = GetKerning(prevChar, c);
                    var uv = GetUV(glyph);

                    float glyphWidth = glyph.Width * scaleX;
                    float glyphHeight = glyph.Height * scaleY;

                    float drawX = currentX + glyph.XOffset * scaleX;
                    float drawY = startY + glyph.YOffset * scaleY;

                    int textureId = GetTextureForPage(glyph.Page);

                    _spriteBatch.Draw(textureId, drawX, drawY, glyphWidth, glyphHeight,
                        uv.u1, uv.v1, uv.u2, uv.v2, r, g, b, a, projection);

                    currentX += (glyph.XAdvance + kern) * scaleX;
                    prevChar = c;
                }
                else
                {
                    currentX += charWidth * scaleX;
                    prevChar = c;
                }
            }
        }

        // ====================== НОВЫЕ УДОБНЫЕ МЕТОДЫ ======================
        public bool TryGetGlyph(char c, out GlyphInfo glyph)
        {
            return _glyphs.TryGetValue(c, out glyph);
        }
        public Vector2 MeasureString(string text, float scaleX, float scaleY)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;
            float width = CalculateTextWidth(text, scaleX);
            return new Vector2(width, _lineHeight * scaleY);
        }

        public Vector2 MeasureString(string text, float scale)
        {
            return MeasureString(text, scale, scale);
        }

        public void DrawStringShadow(string text, float x, float y, float scaleX, float scaleY,float r, float g, float b, float a,Matrix4 projection,TextAlign align = TextAlign.Left,Vector2 shadowOffset = default,float shadowOpacity = 0.6f)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (shadowOffset == default) shadowOffset = new Vector2(3f, 3f);

            // Тень
            DrawString(text, x + shadowOffset.X, y + shadowOffset.Y, scaleX, scaleY,
                       0f, 0f, 0f, a * shadowOpacity, projection, align);

            // Основной текст
            DrawString(text, x, y, scaleX, scaleY, r, g, b, a, projection, align);
        }

        public void DrawStringOutline(string text, float x, float y, float scaleX, float scaleY,float r, float g, float b, float a,Matrix4 projection,TextAlign align = TextAlign.Left,float outlineThickness = 2f,Color4 outlineColor = default)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (outlineColor == default) outlineColor = Color4.Black;

            float t = outlineThickness;

            DrawString(text, x - t, y, scaleX, scaleY, outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A * a, projection, align);
            DrawString(text, x + t, y, scaleX, scaleY, outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A * a, projection, align);
            DrawString(text, x, y - t, scaleX, scaleY, outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A * a, projection, align);
            DrawString(text, x, y + t, scaleX, scaleY, outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A * a, projection, align);

            DrawString(text, x, y, scaleX, scaleY, r, g, b, a, projection, align);
        }
        public (float x, float y, float width, float height)[] GetCharBounds(string text,float baseX,float baseY,Vector2 canvasScale,float baseScale,float scaleMultiply,TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<(float, float, float, float)>();

            float x = baseX * canvasScale.X;
            float y = baseY * canvasScale.Y;
            float finalScaleX = baseScale * canvasScale.X * scaleMultiply;
            float finalScaleY = baseScale * canvasScale.Y * scaleMultiply;

            float textWidth = CalculateTextWidth(text, finalScaleX);
            float startX = align switch
            {
                TextAlign.Center => x - textWidth / 2f,
                TextAlign.Right => x - textWidth,
                _ => x
            };

            var bounds = new (float x, float y, float width, float height)[text.Length];

            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = GetKerning(prevChar, c);

                    float glyphX = currentX + glyph.XOffset * finalScaleX;
                    float glyphY = y + glyph.YOffset * finalScaleY;
                    float glyphWidth = glyph.Width * finalScaleX;
                    float glyphHeight = glyph.Height * finalScaleY;

                    bounds[i] = (glyphX, glyphY, glyphWidth, glyphHeight);

                    currentX += (glyph.XAdvance + kern) * finalScaleX;
                    prevChar = c;
                }
                else
                {
                    // fallback для отсутствующих глифов
                    float fallbackWidth = charWidth * finalScaleX;
                    bounds[i] = (currentX, y, fallbackWidth, _lineHeight * finalScaleY);
                    currentX += fallbackWidth;
                    prevChar = c;
                }
            }

            return bounds;
        }

        public void DrawStringMultiline(string text, float x, float y, float scaleX, float scaleY,float r, float g, float b, float a,Matrix4 projection,TextAlign align = TextAlign.Left,float lineSpacing = 1.2f)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            float currentY = y;

            foreach (var line in lines)
            {
                DrawString(line, x, currentY, scaleX, scaleY, r, g, b, a, projection, align);
                currentY += _lineHeight * scaleY * lineSpacing;
            }
        }

        public float CalculateTextWidth(string text, float scale)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float total = 0f;
            char prev = '\0';

            foreach (char c in text)
            {
                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = GetKerning(prev, c);
                    total += (glyph.XAdvance + kern) * scale;
                    prev = c;
                }
                else
                {
                    total += charWidth * scale;
                    prev = c;
                }
            }
            return total;
        }

        private float CalculateStartX(string text, float scaleX, float baseX, TextAlign align)
        {
            float width = CalculateTextWidth(text, scaleX);
            return align switch
            {
                TextAlign.Center => baseX - width / 2f,
                TextAlign.Right => baseX - width,
                _ => baseX
            };
        }

        public bool TryGetCharIndexAtPoint(string text, float localX, float localY,float scaleX, float scaleY,TextAlign align,out int charIndex)
        {
            charIndex = -1;
            if (string.IsNullOrEmpty(text)) return false;

            float textWidth = CalculateTextWidth(text, scaleX);
            float startX = align switch
            {
                TextAlign.Center => -textWidth * 0.5f,
                TextAlign.Right => -textWidth,
                _ => 0f
            };

            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (!_glyphs.TryGetValue(c, out var glyph))
                {
                    currentX += charWidth * scaleX;
                    prevChar = c;
                    continue;
                }

                float kern = GetKerning(prevChar, c);

                float glyphLeft = currentX + glyph.XOffset * scaleX;
                float glyphRight = glyphLeft + glyph.Width * scaleX;
                float glyphTop = glyph.YOffset * scaleY;
                float glyphBottom = glyphTop + glyph.Height * scaleY;

                if (localX >= glyphLeft && localX <= glyphRight &&
                    localY >= glyphTop && localY <= glyphBottom)
                {
                    charIndex = i;
                    return true;
                }

                currentX += (glyph.XAdvance + kern) * scaleX;
                prevChar = c;
            }

            return false;
        }

    public void DrawStringWithCharColorsScaled(string text,float baseX,float baseY,Vector2 canvasScale,float baseScale,float scaleMultiply,Color4[] charColors,Matrix4 projection,TextAlign align = TextAlign.Center)
        {
            if (string.IsNullOrEmpty(text) || charColors == null || charColors.Length == 0)
                return;

            // Применяем масштаб канваса и ScaleMultiply
            float finalScaleX = baseScale * canvasScale.X * scaleMultiply;
            float finalScaleY = baseScale * canvasScale.Y * scaleMultiply;

            float x = baseX * canvasScale.X;
            float y = baseY * canvasScale.Y;

            float textWidth = CalculateTextWidth(text, finalScaleX);
            float startX = align switch
            {
                TextAlign.Center => x - textWidth / 2f,
                TextAlign.Right => x - textWidth,
                _ => x
            };

            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                Color4 color = i < charColors.Length ? charColors[i] : charColors[^1]; // последний цвет для оставшихся символов

                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = GetKerning(prevChar, c);
                    var uv = GetUV(glyph);

                    float glyphWidth = glyph.Width * finalScaleX;
                    float glyphHeight = glyph.Height * finalScaleY;

                    float drawX = currentX + glyph.XOffset * finalScaleX;
                    float drawY = y + glyph.YOffset * finalScaleY;

                    int textureId = GetTextureForPage(glyph.Page);

                    _spriteBatch.Draw(textureId, drawX, drawY, glyphWidth, glyphHeight,
                        uv.u1, uv.v1, uv.u2, uv.v2,
                        color.R, color.G, color.B, color.A,
                        projection);

                    currentX += (glyph.XAdvance + kern) * finalScaleX;
                    prevChar = c;
                }
                else
                {
                    currentX += charWidth * finalScaleX;
                    prevChar = c;
                }
            }
        }
    }
}