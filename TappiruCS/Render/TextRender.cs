using OpenTK.Mathematics;

namespace TappiruCS.Render
{
    public class TextRender
    {
        private readonly SpriteBatch spriteBatch;

        public readonly Dictionary<int, int> _pageTextures = new();
        private readonly Dictionary<char, GlyphInfo> _glyphs = new();
        public readonly Dictionary<(char first, char second), int> _kerningPairs = new();

        public float _lineHeight;
        public float _scaleW;
        public float _scaleH;

        // Для совместимости
        public float charWidth, charHeight;
        public float texWidth, texHeight;

        public enum TextAlign { Left, Center, Right }

        public struct GlyphInfo
        {
            public int Page;
            public float TexX, TexY, Width, Height;
            public float XOffset, YOffset, XAdvance;
        }
        public bool TryGetCharIndexAtPoint(string text, float localX, float localY,
                                           float scaleX, float scaleY,
                                           TextAlign align,
                                           out int charIndex)
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

                float kern = 0f;
                if (prevChar != '\0' && _kerningPairs.TryGetValue((prevChar, c), out int k))
                    kern = k;

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
        public TextRender(SpriteBatch spriteBatch, string fntPath)
        {
            this.spriteBatch = spriteBatch;
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

                Console.WriteLine($"[BMFont] Page {kvp.Key} → {assetName} (ID={texId})");
            }

            if (_pageTextures.Count == 0)
            {
                Console.WriteLine($"[ERROR] Нет страниц в BMFont: {fntPath}");
                return;
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
            float u1 = g.TexX / texWidth;
            float v1 = g.TexY / texHeight;
            float u2 = (g.TexX + g.Width) / texWidth;
            float v2 = (g.TexY + g.Height) / texHeight;
            return (u1, v1, u2, v2);
        }

        private int GetTextureForPage(int page)
        {
            return _pageTextures.TryGetValue(page, out int id) ? id : (_pageTextures.Count > 0 ? _pageTextures.First().Value : 0);
        }

        // ====================== Основной DrawString (без spacing) ======================
        public void DrawString(string text, float x, float y, float scaleX, float scaleY,
                               float r, float g, float b, float a, Matrix4 projection)
        {
            if (string.IsNullOrEmpty(text)) return;

            float currentX = x;
            char prevChar = '\0';

            foreach (char c in text)
            {
                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = 0f;
                    if (prevChar != '\0' && _kerningPairs.TryGetValue((prevChar, c), out int k))
                        kern = k;

                    var uv = GetUV(glyph);

                    float glyphWidth = glyph.Width * scaleX;
                    float glyphHeight = glyph.Height * scaleY;

                    float drawX = currentX + glyph.XOffset * scaleX;
                    float drawY = y + glyph.YOffset * scaleY;

                    int textureId = GetTextureForPage(glyph.Page);

                    spriteBatch.Draw(textureId, drawX, drawY, glyphWidth, glyphHeight,
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

        public void DrawString(string text, float x, float y, float scaleX, float scaleY,
                               float r, float g, float b, float a, Matrix4 projection, TextAlign align)
        {
            if (string.IsNullOrEmpty(text)) return;

            float textWidth = CalculateTextWidth(text, scaleX);
            float startX = align switch
            {
                TextAlign.Center => x - textWidth / 2f,
                TextAlign.Right => x - textWidth,
                _ => x
            };

            DrawString(text, startX, y, scaleX, scaleY, r, g, b, a, projection);
        }

        public void DrawString(string text, float x, float y, float scale,
                               float r, float g, float b, float a, Matrix4 projection, TextAlign align = TextAlign.Left)
            => DrawString(text, x, y, scale, scale, r, g, b, a, projection, align);

        // ====================== Цветной текст с CanvasScale ======================
        public void DrawStringWithCharColorsScaled(string text,
                                                   float baseX, float baseY,
                                                   Vector2 canvasScale,
                                                   float baseScale,
                                                   float scaleMultiply,
                                                   Color4[] charColors,
                                                   Matrix4 projection,
                                                   TextAlign align = TextAlign.Center)
        {
            if (string.IsNullOrEmpty(text) || charColors == null || charColors.Length == 0) return;

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

            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                Color4 color = i < charColors.Length ? charColors[i] : charColors[^1];

                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = 0f;
                    if (prevChar != '\0' && _kerningPairs.TryGetValue((prevChar, c), out int k))
                        kern = k;

                    var uv = GetUV(glyph);

                    float glyphWidth = glyph.Width * finalScaleX;
                    float glyphHeight = glyph.Height * finalScaleY;

                    float drawX = currentX + glyph.XOffset * finalScaleX;
                    float drawY = y + glyph.YOffset * finalScaleY;

                    int textureId = GetTextureForPage(glyph.Page);

                    spriteBatch.Draw(textureId, drawX, drawY, glyphWidth, glyphHeight,
                        uv.u1, uv.v1, uv.u2, uv.v2,
                        color.R, color.G, color.B, color.A, projection);

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

        public (float x, float y, float width, float height)[] GetCharBounds(string text,
    float baseX, float baseY,
    Vector2 canvasScale,
    float baseScale,
    float scaleMultiply,
    TextAlign align)
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

            var bounds = new (float, float, float, float)[text.Length];
            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = 0f;
                    if (prevChar != '\0' && _kerningPairs.TryGetValue((prevChar, c), out int k))
                        kern = k;

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

        // ====================== CalculateTextWidth (единственная версия) ======================
        public float CalculateTextWidth(string text, float scale)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float total = 0f;
            char prev = '\0';

            foreach (char c in text)
            {
                if (_glyphs.TryGetValue(c, out var glyph))
                {
                    float kern = 0f;
                    if (prev != '\0' && _kerningPairs.TryGetValue((prev, c), out int k))
                        kern = k;

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

        public bool TryGetGlyph(char c, out GlyphInfo glyph)
        {
            return _glyphs.TryGetValue(c, out glyph);
        }

    }
}