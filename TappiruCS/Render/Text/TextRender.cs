using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.Render.Text
{
    public class TextRender
    {
        private readonly SpriteBatch _spriteBatch;
        private Font _currentFont;

        // Статическая ссылка для обратной совместимости
        public static TextRender Instance { get; private set; }

        // Для удобства: свойство, возвращающее текущий шрифт
        public Font CurrentFont => _currentFont;

        public TextRender(SpriteBatch spriteBatch, Font defaultFont)
        {
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _currentFont = defaultFont ?? throw new ArgumentNullException(nameof(defaultFont));
            Instance = this;
        }

        // ========== Прокси-методы к CurrentFont (для обратной совместимости) ==========
        public float BaseLineHeight => _currentFont.LineHeight;
        public float TexWidth => _currentFont.TexWidth;
        public float TexHeight => _currentFont.TexHeight;
        public float CharWidth => _currentFont.CharWidth;
        public float CharHeight => _currentFont.CharHeight;

        public float GetScaleFromFontSize(float fontSize) => _currentFont.GetScaleFromFontSize(fontSize);
        public bool TryGetGlyph(char c, out GlyphInfo glyph) => _currentFont.TryGetGlyph(c, out glyph);
        public int GetKerning(char first, char second) => _currentFont.GetKerning(first, second);
        public int GetTextureForPage(int page) => _currentFont.GetTextureForPage(page);
        public (float, float, float, float) GetUV(in GlyphInfo g) => _currentFont.GetUV(g);
        public float CalculateTextWidth(string text, float scale) => _currentFont.CalculateTextWidth(text, scale);
        public Vector2 MeasureString(string text, float scaleX, float scaleY) => _currentFont.MeasureString(text, scaleX, scaleY);

        public Vector2 MeasureString(string text, float fontSize, float scaleX, float scaleY) =>
            MeasureString(text, GetScaleFromFontSize(fontSize) * scaleX, GetScaleFromFontSize(fontSize) * scaleY);

        // ========== Методы отрисовки (все вместе, без промежуточных функций) ==========

        // Базовая отрисовка (scale)
        public void DrawString(string text, float x, float y, float scaleX, float scaleY,
            float r, float g, float b, float a, Matrix4 projection,
            TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            float startX = CalculateStartX(text, scaleX, x, align);
            DrawStringInternal(text, startX, y, scaleX, scaleY, r, g, b, a, projection);
        }

        // Отрисовка с указанием FontSize
        public void DrawString(string text, float x, float y, float fontSize,
            float scaleX, float scaleY, float r, float g, float b, float a,
            Matrix4 projection, TextAlign align = TextAlign.Left)
        {
            float baseScale = _currentFont.GetScaleFromFontSize(fontSize);
            DrawString(text, x, y, baseScale * scaleX, baseScale * scaleY, r, g, b, a, projection, align);
        }

        public void DrawStringShadow(string text, float x, float y, float scaleX, float scaleY,
            float r, float g, float b, float a, Matrix4 projection,
            TextAlign align = TextAlign.Left,
            Vector2 shadowOffset = default, float shadowOpacity = 0.6f)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (shadowOffset == default) shadowOffset = new Vector2(3f, 3f);

            DrawString(text, x + shadowOffset.X, y + shadowOffset.Y, scaleX, scaleY,
                0, 0, 0, a * shadowOpacity, projection, align);
            DrawString(text, x, y, scaleX, scaleY, r, g, b, a, projection, align);
        }

        public void DrawStringOutline(string text, float x, float y, float scaleX, float scaleY,
            float r, float g, float b, float a, Matrix4 projection,
            TextAlign align = TextAlign.Left,
            float outlineThickness = 2f, Color4 outlineColor = default)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (outlineColor == default) outlineColor = Color4.Black;

            float t = outlineThickness;
            var oc = outlineColor;

            DrawString(text, x - t, y, scaleX, scaleY, oc.R, oc.G, oc.B, oc.A * a, projection, align);
            DrawString(text, x + t, y, scaleX, scaleY, oc.R, oc.G, oc.B, oc.A * a, projection, align);
            DrawString(text, x, y - t, scaleX, scaleY, oc.R, oc.G, oc.B, oc.A * a, projection, align);
            DrawString(text, x, y + t, scaleX, scaleY, oc.R, oc.G, oc.B, oc.A * a, projection, align);

            DrawString(text, x, y, scaleX, scaleY, r, g, b, a, projection, align);
        }

        public void DrawStringWithCharColors(string text, float baseX, float baseY,
            float fontSize, float scaleX, float scaleY,
            Color4[] charColors, Matrix4 projection,
            TextAlign align = TextAlign.Center)
        {
            if (string.IsNullOrEmpty(text) || charColors == null || charColors.Length == 0) return;

            float baseScale = _currentFont.GetScaleFromFontSize(fontSize);
            float fsX = baseScale * scaleX;
            float fsY = baseScale * scaleY;

            float startX = CalculateStartX(text, fsX, baseX, align);

            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                Color4 color = i < charColors.Length ? charColors[i] : charColors[^1];

                if (_currentFont.TryGetGlyph(c, out var glyph))
                {
                    float kern = _currentFont.GetKerning(prevChar, c);
                    var uv = _currentFont.GetUV(glyph);

                    float gw = glyph.Width * fsX;
                    float gh = glyph.Height * fsY;
                    float dx = currentX + glyph.XOffset * fsX;
                    float dy = baseY + glyph.YOffset * fsY;

                    int texId = _currentFont.GetTextureForPage(glyph.Page);
                    _spriteBatch.Draw(texId, dx, dy, gw, gh,
                        uv.u1, uv.v1, uv.u2, uv.v2,
                        color.R, color.G, color.B, color.A, projection);

                    currentX += (glyph.XAdvance + kern) * fsX;
                    prevChar = c;
                }
                else
                {
                    currentX += _currentFont.CharWidth * fsX;
                    prevChar = c;
                }
            }
        }

        // ========== Методы запросов (bounds, hit-test) ==========

        public (float x, float y, float width, float height)[] GetCharBounds(
            string text,
            float baseX, float baseY,
            Vector2 canvasScale,
            float baseScale, float scaleMultiply,
            TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<(float, float, float, float)>();

            float x = baseX * canvasScale.X;
            float y = baseY * canvasScale.Y;
            float finalScaleX = baseScale * canvasScale.X * scaleMultiply;
            float finalScaleY = baseScale * canvasScale.Y * scaleMultiply;

            float startX = CalculateStartX(text, finalScaleX, x, align);

            var bounds = new (float x, float y, float width, float height)[text.Length];
            float currentX = startX;
            char prevChar = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (_currentFont.TryGetGlyph(c, out var glyph))
                {
                    float kern = _currentFont.GetKerning(prevChar, c);
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
                    float fallbackWidth = _currentFont.CharWidth * finalScaleX;
                    bounds[i] = (currentX, y, fallbackWidth, _currentFont.LineHeight * finalScaleY);
                    currentX += fallbackWidth;
                    prevChar = c;
                }
            }
            return bounds;
        }

        public bool TryGetCharIndexAtPoint(
            string text,
            float localX, float localY,
            float scaleX, float scaleY,
            TextAlign align,
            out int charIndex)
        {
            charIndex = -1;
            if (string.IsNullOrEmpty(text)) return false;

            float textWidth = _currentFont.CalculateTextWidth(text, scaleX);
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
                if (!_currentFont.TryGetGlyph(c, out var glyph))
                {
                    currentX += _currentFont.CharWidth * scaleX;
                    prevChar = c;
                    continue;
                }

                float kern = _currentFont.GetKerning(prevChar, c);
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

        // ========== Внутренние вспомогательные методы (в самом конце) ==========

        private void DrawStringInternal(string text, float startX, float startY,
            float scaleX, float scaleY, float r, float g, float b, float a, Matrix4 projection)
        {
            float currentX = startX;
            char prevChar = '\0';

            foreach (char c in text)
            {
                if (_currentFont.TryGetGlyph(c, out var glyph))
                {
                    float kern = _currentFont.GetKerning(prevChar, c);
                    var uv = _currentFont.GetUV(glyph);

                    float gw = glyph.Width * scaleX;
                    float gh = glyph.Height * scaleY;
                    float dx = currentX + glyph.XOffset * scaleX;
                    float dy = startY + glyph.YOffset * scaleY;

                    int texId = _currentFont.GetTextureForPage(glyph.Page);
                    _spriteBatch.Draw(texId, dx, dy, gw, gh,
                        uv.u1, uv.v1, uv.u2, uv.v2, r, g, b, a, projection);

                    currentX += (glyph.XAdvance + kern) * scaleX;
                    prevChar = c;
                }
                else
                {
                    currentX += _currentFont.CharWidth * scaleX;
                    prevChar = c;
                }
            }
        }

        private float CalculateStartX(string text, float scaleX, float baseX, TextAlign align)
        {
            float width = _currentFont.CalculateTextWidth(text, scaleX);
            return align switch
            {
                TextAlign.Center => baseX - width / 2f,
                TextAlign.Right => baseX - width,
                _ => baseX
            };
        }
    }
}