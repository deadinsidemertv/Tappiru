using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Diagnostics;
using TappiruCS.Server;
using TappiruCS.UI;

namespace TappiruCS.Render
{
    /// <summary>
    /// Простой immediate-mode quad drawer (не настоящий батчер, но очень удобный).
    /// </summary>
    public class SpriteBatch
    {
        private readonly int _shaderProgram;
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _ebo;

        // Закэшированные locations шейдера
        private readonly int _projectionLoc;
        private readonly int _texLoc;
        private readonly int _colorLoc;

        // Кэш размеров текстур — чтобы не делать GL.GetTexLevelParameter каждый кадр
        private readonly Dictionary<int, (int w, int h)> _texSizeCache = new();

        public SpriteBatch(int shaderProgram)
        {
            _shaderProgram = shaderProgram;
            _projectionLoc = GL.GetUniformLocation(shaderProgram, "projection");
            _texLoc = GL.GetUniformLocation(shaderProgram, "tex");
            _colorLoc = GL.GetUniformLocation(shaderProgram, "color");

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        // ── Получить размер текстуры (с кэшем) ───────────────────────────────────
        private (int w, int h) GetTextureSize(int textureId)
        {
            if (_texSizeCache.TryGetValue(textureId, out var cached))
                return cached;

            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int w);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int h);

            var size = (w, h);
            _texSizeCache[textureId] = size;
            return size;
        }

        /// <summary>
        /// Вызывать при удалении/замене текстуры, чтобы сбросить кэш её размера.
        /// </summary>
        public void InvalidateTextureSize(int textureId)
            => _texSizeCache.Remove(textureId);

        // ── Draw ──────────────────────────────────────────────────────────────────

        public void Draw(int textureId,
                         float x, float y, float width, float height,
                         float u1, float v1, float u2, float v2,
                         float r, float g, float b, float a,
                         Matrix4 projection)
        {
            float[] vertices = CreateQuadVertices(x, y, width, height, u1, v1, u2, v2);
            UploadVertices(vertices);
            BindAndSetUniforms(textureId, r, g, b, a, projection);
            DrawElements();
        }

        public void DrawRect(float x, float y, float width, float height, Color4 color, Matrix4 projection)
        {
            int whiteTex = TextureManager.GetTexture("white");
            Draw(whiteTex, x, y, width, height,
                 0f, 0f, 1f, 1f,
                 color.R, color.G, color.B, color.A,
                 projection);
        }

        // ── Draw9Slice ────────────────────────────────────────────────────────────

        public void Draw9Slice(int textureId,
                               float x, float y, float width, float height,
                               Vector4 sliceBorders, Color4 color, Matrix4 projection)
        {
            float left = sliceBorders.X;
            float right = sliceBorders.Y;
            float top = sliceBorders.Z;
            float bottom = sliceBorders.W;

            // Защита: если объект меньше суммы бордюров — рисуем как обычный спрайт
            if (width <= 0 || height <= 0 ||
                width < left + right || height < top + bottom)
            {
                Draw(textureId, x, y, width, height, 0f, 0f, 1f, 1f,
                     color.R, color.G, color.B, color.A, projection);
                return;
            }

            // Размер текстуры — из кэша, без GPU roundtrip каждый кадр
            var (texWidth, texHeight) = GetTextureSize(textureId);
            if (texWidth <= 0 || texHeight <= 0) return;

            float invW = 1f / texWidth;
            float invH = 1f / texHeight;

            // UV координаты 4 вертикальных и 4 горизонтальных линий
            float u0 = 0f, u1 = left * invW,
                  u2 = 1f - right * invW, u3 = 1f;

            float v0 = 0f, v1 = top * invH,
                  v2 = 1f - bottom * invH, v3 = 1f;

            // Экранные координаты 4 вертикальных и 4 горизонтальных линий
            float x0 = x, x1 = x + left,
                  x2 = x + width - right, x3 = x + width;

            float y0 = y, y1 = y + top,
                  y2 = y + height - bottom, y3 = y + height;

            float r = color.R, g = color.G, b = color.B, a = color.A;

            // 9 квадратов (углы → стороны → центр)
            // Верхняя строка
            DrawQuad(textureId, x0, y0, x1 - x0, y1 - y0, u0, v0, u1, v1, r, g, b, a, projection);
            DrawQuad(textureId, x1, y0, x2 - x1, y1 - y0, u1, v0, u2, v1, r, g, b, a, projection);
            DrawQuad(textureId, x2, y0, x3 - x2, y1 - y0, u2, v0, u3, v1, r, g, b, a, projection);
            // Средняя строка
            DrawQuad(textureId, x0, y1, x1 - x0, y2 - y1, u0, v1, u1, v2, r, g, b, a, projection);
            DrawQuad(textureId, x1, y1, x2 - x1, y2 - y1, u1, v1, u2, v2, r, g, b, a, projection);
            DrawQuad(textureId, x2, y1, x3 - x2, y2 - y1, u2, v1, u3, v2, r, g, b, a, projection);
            // Нижняя строка
            DrawQuad(textureId, x0, y2, x1 - x0, y3 - y2, u0, v2, u1, v3, r, g, b, a, projection);
            DrawQuad(textureId, x1, y2, x2 - x1, y3 - y2, u1, v2, u2, v3, r, g, b, a, projection);
            DrawQuad(textureId, x2, y2, x3 - x2, y3 - y2, u2, v2, u3, v3, r, g, b, a, projection);
        }

        private void DrawQuad(int textureId,
                              float x, float y, float w, float h,
                              float u1, float v1, float u2, float v2,
                              float r, float g, float b, float a,
                              Matrix4 projection)
        {
            if (w <= 0.001f || h <= 0.001f) return;

            float[] vertices = CreateQuadVertices(x, y, w, h, u1, v1, u2, v2);
            UploadVertices(vertices);
            BindAndSetUniforms(textureId, r, g, b, a, projection);
            DrawElements();
        }

        // ── Приватные хелперы ─────────────────────────────────────────────────────

        private static float[] CreateQuadVertices(float x, float y, float w, float h,
                                                  float u1, float v1, float u2, float v2)
        {
            return new[]
            {
                x,     y,     u1, v1,
                x + w, y,     u2, v1,
                x + w, y + h, u2, v2,
                x,     y + h, u1, v2
            };
        }

        private void UploadVertices(float[] vertices)
        {
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
        }

        private void BindAndSetUniforms(int textureId, float r, float g, float b, float a, Matrix4 projection)
        {
            GL.UseProgram(_shaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            if (_texLoc != -1) GL.Uniform1(_texLoc, 0);
            if (_colorLoc != -1) GL.Uniform4(_colorLoc, r, g, b, a);
            if (_projectionLoc != -1) GL.UniformMatrix4(_projectionLoc, false, ref projection);
        }

        private static void DrawElements()
        {
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        // ── Glow ──────────────────────────────────────────────────────────────────

        public void DrawGlow(int textureId,
                             float x, float y, float width, float height,
                             float r, float g, float b, float baseAlpha,
                             float glowIntensity, Matrix4 projection,
                             int steps = 5, float spread = 4f)
        {
            if (glowIntensity <= 0f) return;

            for (int i = steps; i >= 1; i--)
            {
                float offset = spread * (i / (float)steps);
                float alpha = baseAlpha * glowIntensity * (0.12f / i);

                Draw(textureId,
                     x - offset, y - offset, width + offset * 2f, height + offset * 2f,
                     0f, 0f, 1f, 1f,
                     r, g, b, alpha,
                     projection);
            }
        }

        public void DrawGlowRect(float x, float y, float width, float height, Color4 color,
                                 float baseAlpha, Matrix4 projection,
                                 int steps = 5, float spread = 8f)
        {
            if (baseAlpha <= 0f) return;

            for (int i = steps; i >= 1; i--)
            {
                float offset = spread * (i / (float)steps);
                float alpha = baseAlpha * (0.14f / i);

                DrawRect(x - offset, y - offset, width + offset * 2f, height + offset * 2f,
                         new Color4(color.R, color.G, color.B, alpha), projection);
            }
        }

        // ── Dispose ───────────────────────────────────────────────────────────────

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }
    }
}