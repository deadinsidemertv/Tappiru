using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpFont;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static TappiruCS.Render.Text.BMFont.Font;

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
        public int CurrentSize { get; private set; }

        public FreeTypeRender(SpriteBatch sb, string fontPath, int pixelSize = 48)
        {
            _spriteBatch = sb;
            _library ??= new Library();
            _face = new Face(_library, fontPath, 0);
            SetSize(pixelSize);
        }

        public void SetSize(int pixelSize)
        {
            CurrentSize = pixelSize;
            _face.SetPixelSizes(0, (uint)pixelSize);
            var m = _face.Size.Metrics;
            LineHeight = m.Height.ToSingle() / 64f;
            Ascender = m.Ascender.ToSingle();
        }

        public float GetScaleFromFontSize(float fontSize) => fontSize / CurrentSize;

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

        private FreeTypeGlyph? RenderGlyphToTexture(uint codepoint)
        {
            try
            {
                _face.LoadChar(codepoint, LoadFlags.Render, LoadTarget.Normal);
                var slot = _face.Glyph;
                var bitmap = slot.Bitmap;

                float xAdvance = slot.Advance.X.ToSingle();

                // Пробел и символы без растра — только advance, без текстуры
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

                // Grayscale → RGBA
                byte[] rgba = new byte[width * height * 4];
                IntPtr src = bitmap.Buffer;
                for (int i = 0; i < width * height; i++)
                {
                    byte alpha = Marshal.ReadByte(src, i);
                    int d = i * 4;
                    rgba[d] = 255;
                    rgba[d + 1] = 255;
                    rgba[d + 2] = 255;
                    rgba[d + 3] = alpha;
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
            float w = 0f;
            char prev = '\0';
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

        // ── Хит-тест символа ──────────────────────────────────────────────────────

        public bool TryGetCharIndexAtPoint(
            string text, float localX, float localY,
            float scaleX, float scaleY,
            TextAlign align, out int charIndex)
        {
            charIndex = -1;
            float pen = 0f;
            char prev = '\0';
            int idx = 0;
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
                prev = c;
                idx++;
            }
            return false;
        }

        // ── DrawString ────────────────────────────────────────────────────────────
        //
        // SpriteBatch: Y идёт ВНИЗ.
        // startY — Y базовой линии.
        //
        // xPos = penX + BearingX * scaleX
        // yPos = startY - BearingY * scaleY   (BearingY > 0 = вверх от baseline,
        //                                       но экран вниз, поэтому минус)
        //
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
                if (!TryGetRenderedGlyph(c, out var glyph) || glyph == null)
                    continue;

                var info = glyph.Info;

                if (prev != '\0') penX += GetKerning(prev, c) * scaleX;

                if (glyph.TextureId > 0 && info.Width > 0 && info.Height > 0)
                {
                    float w = info.Width * scaleX;
                    float h = info.Height * scaleY;
                    float xPos = penX + info.BearingX * scaleX;
                    float yPos = startY - info.BearingY * scaleY;

                    _spriteBatch.Draw(
                        glyph.TextureId,
                        xPos, yPos, w, h,
                        0f, 0f, 1f, 1f,
                        r, g, b, a,
                        projection);
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
            float[] offsets = { -1f, 0f, 1f };
            foreach (float dx in offsets)
                foreach (float dy in offsets)
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

        // ── Dispose ────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            foreach (var glyph in _glyphCache.Values)
                if (glyph != null && glyph.TextureId > 0)
                    GL.DeleteTexture(glyph.TextureId);
            _face?.Dispose();
        }
    }
}