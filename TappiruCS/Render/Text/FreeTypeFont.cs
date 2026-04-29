using SharpFont;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace TappiruCS.Render.Text
{
    public class FreeTypeFont : IDisposable
    {
        private static Library? _library;
        private readonly Face _face;
        private readonly Dictionary<uint, FreeTypeGlyph> _glyphCache = new();

        public float LineHeight { get; private set; }
        public int CurrentSize { get; private set; }

        public FreeTypeFont(string fontPath, int pixelSize = 48)
        {
            if (_library == null)
                _library = new Library();

            _face = new Face(_library, fontPath, 0);
            SetSize(pixelSize);
        }

        public void SetSize(int pixelSize)
        {
            CurrentSize = pixelSize;
            _face.SetPixelSizes(0, (uint)pixelSize);
            LineHeight = _face.Size.Metrics.Height.ToSingle() / 64f;
        }

        // Новый главный метод
        public bool TryGetRenderedGlyph(char c, out FreeTypeGlyph glyph)
        {
            uint codepoint = (uint)c;

            if (_glyphCache.TryGetValue(codepoint, out glyph))
                return true;

            glyph = RenderGlyphToTexture(codepoint);
            _glyphCache[codepoint] = glyph;
            return glyph != null;
        }

        private FreeTypeGlyph? RenderGlyphToTexture(uint codepoint)
        {
            try
            {
                _face.LoadChar(codepoint, LoadFlags.Render, LoadTarget.Normal);

                var slot = _face.Glyph;
                var bitmap = slot.Bitmap;

                if (bitmap.Width == 0 || bitmap.Rows == 0)
                    return null;

                // Создаём RGBA буфер вручную
                int width = bitmap.Width;
                int height = bitmap.Rows;
                byte[] rgbaData = new byte[width * height * 4];

                // Копируем grayscale данные FreeType в RGBA (белый цвет + альфа из bitmap)
                IntPtr srcPtr = bitmap.Buffer;
                for (int i = 0; i < width * height; i++)
                {
                    byte alpha = Marshal.ReadByte(srcPtr, i);
                    int destIndex = i * 4;
                    rgbaData[destIndex + 0] = 255;     // R
                    rgbaData[destIndex + 1] = 255;     // G
                    rgbaData[destIndex + 2] = 255;     // B
                    rgbaData[destIndex + 3] = alpha;   // A
                }

                int textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, textureId);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              width, height, 0,
                              PixelFormat.Rgba, PixelType.UnsignedByte, rgbaData);

                Console.WriteLine($"Glyph texture: {width}x{height}, first pixel alpha={rgbaData[3]}");

                var glyphInfo = new GlyphInfo
                {
                    Width = width,
                    Height = height,
                    XOffset = slot.BitmapLeft,
                    YOffset = slot.BitmapTop - height,
                    XAdvance = slot.Advance.X.ToSingle() / 64f,
                    BearingX = slot.BitmapLeft,
                    BearingY = slot.BitmapTop,
                    Page = textureId,
                    TexX = 0,
                    TexY = 0
                };

                return new FreeTypeGlyph(glyphInfo, textureId, new Vector2(0, 0), new Vector2(1, 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FreeType] Ошибка рендеринга глифа {codepoint}: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            foreach (var glyph in _glyphCache.Values)
            {
                if (glyph.TextureId > 0)
                    GL.DeleteTexture(glyph.TextureId);
            }
            _face?.Dispose();
        }
    }
}