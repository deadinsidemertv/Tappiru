using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TappiruCS.Render.Text.FreeType
{
    public class FreeTypeRender : IDisposable
    {
        private static Library? _library;
        private readonly Face _face;
        private readonly Dictionary<uint, FreeTypeGlyph?> _glyphCache = new();
        private readonly SpriteBatch _spriteBatch;

        public float LineHeight { get; private set; }
        public float Ascender { get; private set; }
        public float Descender { get; private set; } // отрицательное значение в FreeType

        public int CurrentSize { get; private set; }

        public FreeTypeRender(SpriteBatch sb, string fontPath, int pixelSize = 48)
        {
            _spriteBatch = sb;
            _library ??= new Library();
            _face = new Face(_library, fontPath, 0);
            SetSize(pixelSize);
        }

        // ── Размер ────────────────────────────────────────────────────────────────

        public void SetSize(int pixelSize)
        {
            CurrentSize = pixelSize;
            _face.SetPixelSizes(0, (uint)pixelSize);
            var m = _face.Size.Metrics;
            LineHeight = m.Height.ToSingle();
            Ascender = m.Ascender.ToSingle();
            Descender = m.Descender.ToSingle(); // < 0
        }

        /// <summary>
        /// Возвращает Descender (отрицательное число в FreeType метриках).
        /// Используется для вертикального центрирования текста.
        /// </summary>
        public float GetDescender() => Descender;

        /// <summary>
        /// Возвращает scale такой, что текст с fontSize визуально совпадает
        /// с высотой строки (как было в BMFont).
        /// </summary>
        public float GetScaleFromFontSize(float fontSize) => fontSize / LineHeight;

        // ── Кэш глифов ────────────────────────────────────────────────────────────

        public bool TryGetRenderedGlyph(char c, out FreeTypeGlyph? glyph)
        {
            uint cp = (uint)c;
            if (_glyphCache.TryGetValue(cp, out glyph))
                return glyph != null;
            glyph = RenderGlyphToTexture(cp);
            _glyphCache[cp] = glyph;
            return glyph != null;
        }

        /// <summary>Совместимость с PhraseDisplayRenderer — возвращает GlyphInfo.</summary>
        public bool TryGetGlyph(char c, out GlyphInfo info)
        {
            if (TryGetRenderedGlyph(c, out var g) && g != null)
            {
                info = g.Info;
                return true;
            }
            info = GlyphInfo.Empty;
            return false;
        }

        private FreeTypeGlyph? RenderGlyphToTexture(uint codepoint)
        {
            try
            {
                _face.LoadChar(codepoint, LoadFlags.Render, LoadTarget.Normal);
                var slot = _face.Glyph;
                var bitmap = slot.Bitmap;

                float xAdvance = slot.Advance.X.ToSingle();

                if (bitmap.Width == 0 || bitmap.Rows == 0)
                {
                    var empty = new GlyphInfo
                    {
                        Width = 0,
                        Height = 0,
                        BearingX = slot.BitmapLeft,
                        BearingY = slot.BitmapTop,
                        XAdvance = xAdvance,
                    };
                    return new FreeTypeGlyph(empty, 0, Vector2.Zero, Vector2.Zero);
                }

                int width = bitmap.Width;
                int height = bitmap.Rows;

                byte[] rgba = new byte[width * height * 4];
                IntPtr src = bitmap.Buffer;
                for (int i = 0; i < width * height; i++)
                {
                    byte a = Marshal.ReadByte(src, i);
                    int d = i * 4;
                    rgba[d] = rgba[d + 1] = rgba[d + 2] = 255;
                    rgba[d + 3] = a;
                }

                int tex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tex);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);

                var info = new GlyphInfo
                {
                    Width = width,
                    Height = height,
                    BearingX = slot.BitmapLeft,
                    BearingY = slot.BitmapTop,
                    XAdvance = xAdvance,
                    Page = tex,
                };
                return new FreeTypeGlyph(info, tex, Vector2.Zero, Vector2.One);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FreeType] Ошибка {codepoint}: {ex.Message}");
                return null;
            }
        }

        // ── Kerning ────────────────────────────────────────────────────────────────

        public float GetKerning(char left, char right)
        {
            if (!_face.HasKerning) return 0f;
            uint li = _face.GetCharIndex((uint)left);
            uint ri = _face.GetCharIndex((uint)right);
            if (li == 0 || ri == 0) return 0f;
            return _face.GetKerning(li, ri, KerningMode.Default).X.ToSingle();
        }

        // ── Ширина строки ──────────────────────────────────────────────────────────

        public float CalculateTextWidth(string text, float scaleX)
        {
            float w = 0f; char prev = '\0';
            foreach (char c in text)
            {
                if (TryGetRenderedGlyph(c, out var g) && g != null)
                {
                    if (prev != '\0') w += GetKerning(prev, c) * scaleX;
                    w += g.Info.XAdvance * scaleX;
                }
                prev = c;
            }
            return w;
        }

        // ── GetCharBounds ──────────────────────────────────────────────────────────

        public (float x, float y, float width, float height)[]? GetCharBounds(
            string text,
            float centerX, float centerY,
            Vector2 canvasScale,
            float baseScale, float scaleMultiply,
            TextAlign align)
        {
            if (string.IsNullOrEmpty(text)) return null;

            float scaleX = baseScale * scaleMultiply * canvasScale.X;
            float scaleY = baseScale * scaleMultiply * canvasScale.Y;

            float screenX = centerX * canvasScale.X;
            float screenY = centerY * canvasScale.Y;

            float totalWidth = CalculateTextWidth(text, scaleX);
            float penX = screenX;
            switch (align)
            {
                case TextAlign.Center: penX -= totalWidth * 0.5f; break;
                case TextAlign.Right: penX -= totalWidth; break;
            }

            var result = new (float x, float y, float width, float height)[text.Length];
            char prev = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (TryGetRenderedGlyph(c, out var g) && g != null)
                {
                    if (prev != '\0') penX += GetKerning(prev, c) * scaleX;

                    float x = penX + g.Info.BearingX * scaleX;
                    float y = screenY - g.Info.BearingY * scaleY;
                    float w = g.Info.Width * scaleX;
                    float h = g.Info.Height * scaleY;

                    result[i] = (x, y, w, h);
                    penX += g.Info.XAdvance * scaleX;
                }
                else
                {
                    result[i] = (penX, screenY, 0f, 0f);
                }
                prev = c;
            }
            return result;
        }

        // ── Хит-тест символа ──────────────────────────────────────────────────────

        public bool TryGetCharIndexAtPoint(
            string text, float localX, float localY,
            float scaleX, float scaleY,
            TextAlign align, out int charIndex)
        {
            charIndex = -1;
            float pen = 0f; char prev = '\0'; int idx = 0;
            foreach (char c in text)
            {
                if (TryGetRenderedGlyph(c, out var g) && g != null)
                {
                    if (prev != '\0') pen += GetKerning(prev, c) * scaleX;
                    float x0 = pen + g.Info.BearingX * scaleX;
                    float x1 = x0 + g.Info.Width * scaleX;
                    if (localX >= x0 && localX < x1) { charIndex = idx; return true; }
                    pen += g.Info.XAdvance * scaleX;
                }
                prev = c; idx++;
            }
            return false;
        }

        // ── DrawString ────────────────────────────────────────────────────────────

        public void DrawString(
            string text, float startX, float startY,
            float scaleX, float scaleY,
            float r, float g, float b, float a,
            Matrix4 projection,
            TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            float totalWidth = CalculateTextWidth(text, scaleX);
            float penX = startX;
            switch (align)
            {
                case TextAlign.Center: penX -= totalWidth * 0.5f; break;
                case TextAlign.Right: penX -= totalWidth; break;
            }

            char prev = '\0';
            foreach (char c in text)
            {
                if (!TryGetRenderedGlyph(c, out var glyph) || glyph == null) continue;
                var info = glyph.Info;

                if (prev != '\0') penX += GetKerning(prev, c) * scaleX;

                if (glyph.TextureId > 0 && info.Width > 0 && info.Height > 0)
                {
                    _spriteBatch.Draw(
                        glyph.TextureId,
                        penX + info.BearingX * scaleX,
                        startY - info.BearingY * scaleY,
                        info.Width * scaleX, info.Height * scaleY,
                        0f, 0f, 1f, 1f,
                        r, g, b, a, projection);
                }

                penX += info.XAdvance * scaleX;
                prev = c;
            }
        }

        // ── DrawStringWithCharColors ───────────────────────────────────────────────

        public void DrawStringWithCharColors(
            string text,
            float startX, float startY,
            float baseFontSize,
            float scaleX, float scaleY,
            Color4[] colors,
            Matrix4 projection,
            TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text) || colors == null) return;

            float totalWidth = CalculateTextWidth(text, scaleX);
            float penX = startX;
            switch (align)
            {
                case TextAlign.Center: penX -= totalWidth * 0.5f; break;
                case TextAlign.Right: penX -= totalWidth; break;
            }

            char prev = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!TryGetRenderedGlyph(c, out var glyph) || glyph == null) continue;

                var info = glyph.Info;
                Color4 color = i < colors.Length ? colors[i] : Color4.White;

                if (prev != '\0') penX += GetKerning(prev, c) * scaleX;

                if (glyph.TextureId > 0 && info.Width > 0 && info.Height > 0)
                {
                    _spriteBatch.Draw(
                        glyph.TextureId,
                        penX + info.BearingX * scaleX,
                        startY - info.BearingY * scaleY,
                        info.Width * scaleX, info.Height * scaleY,
                        0f, 0f, 1f, 1f,
                        color.R, color.G, color.B, color.A, projection);
                }

                penX += info.XAdvance * scaleX;
                prev = c;
            }
        }

        // ── Shadow / Outline ───────────────────────────────────────────────────────

        public void DrawStringShadow(
            string text, float startX, float startY,
            float scaleX, float scaleY,
            float r, float g, float b, float a,
            Matrix4 projection, TextAlign align,
            Vector2 shadowOffset, float shadowOpacity)
        {
            DrawString(text,
                startX + shadowOffset.X, startY + shadowOffset.Y,
                scaleX, scaleY, 0f, 0f, 0f, a * shadowOpacity,
                projection, align);
            DrawString(text, startX, startY, scaleX, scaleY, r, g, b, a, projection, align);
        }

        public void DrawStringOutline(
            string text, float startX, float startY,
            float scaleX, float scaleY,
            float r, float g, float b, float a,
            Matrix4 projection, TextAlign align,
            float thickness, Color4 outlineColor)
        {
            float[] d = { -1f, 0f, 1f };
            foreach (float dx in d)
                foreach (float dy in d)
                {
                    if (dx == 0f && dy == 0f) continue;
                    DrawString(text,
                        startX + dx * thickness, startY + dy * thickness,
                        scaleX, scaleY,
                        outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A,
                        projection, align);
                }
            DrawString(text, startX, startY, scaleX, scaleY, r, g, b, a, projection, align);
        }

        // ── Вспомогательные ───────────────────────────────────────────────────────

        public void DrawSingleGlyph(
            char c,
            float screenX, float screenY,
            float scaleX, float scaleY,
            float r, float g, float b, float a,
            Matrix4 projection)
        {
            if (!TryGetRenderedGlyph(c, out var glyph) || glyph == null || glyph.TextureId <= 0)
                return;

            var info = glyph.Info;
            if (info.Width <= 0 || info.Height <= 0) return;

            _spriteBatch.Draw(
                glyph.TextureId,
                screenX, screenY,
                info.Width * scaleX, info.Height * scaleY,
                0f, 0f, 1f, 1f,
                r, g, b, a, projection);
        }

        public float GetBaselineOffset(char c)
        {
            if (!TryGetRenderedGlyph(c, out var glyph) || glyph == null)
                return Ascender * 0.7f;
            return glyph.Info.BearingY;
        }

        public float GetBearingY(char c)
        {
            if (TryGetRenderedGlyph(c, out var glyph) && glyph != null)
                return glyph.Info.BearingY;
            return Ascender * 0.7f;
        }

        public Vector2 MeasureString(string text, float scaleX, float scaleY)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;
            return new Vector2(
                CalculateTextWidth(text, scaleX),
                LineHeight * scaleY);
        }

        public RectangleF GetTextBounds(
            string text,
            float anchorX, float anchorY,
            float baseScale, float scaleMultiply,
            Vector2 canvasScale,
            TextAlign align = TextAlign.Left,
            Vector2? pivot = null)
        {
            if (string.IsNullOrEmpty(text))
                return new RectangleF(anchorX, anchorY, 0, 0);

            float scaleX = baseScale * scaleMultiply * canvasScale.X;
            float scaleY = baseScale * scaleMultiply * canvasScale.Y;

            float textWidth = CalculateTextWidth(text, scaleX);
            if (textWidth <= 0)
                return new RectangleF(anchorX, anchorY, 0, LineHeight * scaleY);

            float penX = anchorX;
            switch (align)
            {
                case TextAlign.Center: penX -= textWidth * 0.5f; break;
                case TextAlign.Right: penX -= textWidth; break;
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            char prev = '\0';
            float currentPenX = penX;

            foreach (char c in text)
            {
                if (TryGetRenderedGlyph(c, out var glyph) && glyph != null)
                {
                    if (prev != '\0')
                        currentPenX += GetKerning(prev, c) * scaleX;

                    float glyphLeft = currentPenX + glyph.Info.BearingX * scaleX;
                    float glyphRight = glyphLeft + glyph.Info.Width * scaleX;
                    float glyphTop = anchorY - glyph.Info.BearingY * scaleY;
                    float glyphBottom = glyphTop + glyph.Info.Height * scaleY;

                    minX = Math.Min(minX, glyphLeft);
                    maxX = Math.Max(maxX, glyphRight);
                    minY = Math.Min(minY, glyphTop);
                    maxY = Math.Max(maxY, glyphBottom);

                    currentPenX += glyph.Info.XAdvance * scaleX;
                }
                else
                {
                    currentPenX += CalculateTextWidth(c.ToString(), scaleX);
                }
                prev = c;
            }

            float boundsWidth = maxX - minX;
            float boundsHeight = maxY - minY;

            if (pivot.HasValue)
            {
                minX += boundsWidth * (0.5f - pivot.Value.X);
                minY += boundsHeight * (0.5f - pivot.Value.Y);
            }

            return new RectangleF(minX, minY, boundsWidth, boundsHeight);
        }

        // ── Dispose ────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            foreach (var g in _glyphCache.Values)
                if (g != null && g.TextureId > 0)
                    GL.DeleteTexture(g.TextureId);
            _face?.Dispose();
        }
    }
}