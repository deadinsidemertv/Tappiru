using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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

        public SpriteBatch(int shaderProgram)
        {
            _shaderProgram = shaderProgram;
            // Кэшируем uniform'ы один раз
            _projectionLoc = GL.GetUniformLocation(shaderProgram, "projection");
            _texLoc = GL.GetUniformLocation(shaderProgram, "tex");
            _colorLoc = GL.GetUniformLocation(shaderProgram, "color");

            // === VAO / VBO / EBO ===
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            // Индексы (всегда одни и те же)
            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // VBO (данные будут обновляться каждый Draw)
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            // Атрибуты: WorldPosition (2) + UV (2)
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            
        }

        public void Draw(int textureId, float x, float y, float width, float height,float u1, float v1, float u2, float v2,float r, float g, float b, float a,Matrix4 projection)
        {
            float[] vertices = CreateQuadVertices(x, y, width, height, u1, v1, u2, v2);

            // 2. Загрузка данных в GPU
            UploadVertices(vertices);

            // 3. Установка состояния OpenGL + uniform'ов
            BindAndSetUniforms(textureId, r, g, b, a, projection);

            // 4. Сама отрисовка
            DrawElements();
        }
        public void DrawRect(float x, float y, float width, float height, Color4 color, Matrix4 projection)
        {
            int whiteTex = TextureManager.GetTexture("white"); // должна быть в TextureManager

            Draw(whiteTex, x, y, width, height,
                 0f, 0f, 1f, 1f,
                 color.R, color.G, color.B, color.A,
                 projection);
        }

        public void Draw9Slice(int textureId, float x, float y, float width, float height,
                       Vector4 sliceBorders, Color4 color, Matrix4 projection)
        {
            float left = sliceBorders.X;
            float top = sliceBorders.Z;
            float right = sliceBorders.Y;
            float bottom = sliceBorders.W;

            if (width <= 0 || height <= 0 ||
                width < left + right || height < top + bottom)
            {
                Draw(textureId, x, y, width, height, 0, 0, 1, 1,
                     color.R, color.G, color.B, color.A, projection);
                return;
            }

            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int texWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int texHeight);

            float invW = 1f / texWidth;
            float invH = 1f / texHeight;

            float u0 = 0f;
            float u1 = left * invW;
            float u2 = 1f - right * invW;
            float u3 = 1f;

            float v0 = 0f;
            float v1 = top * invH;
            float v2 = 1f - bottom * invH;
            float v3 = 1f;

            float x0 = x;
            float x1 = x + left;
            float x2 = x + width - right;
            float x3 = x + width;

            float y0 = y;
            float y1 = y + top;
            float y2 = y + height - bottom;
            float y3 = y + height;

            // 9 квадратов
            DrawQuad(textureId, x0, y0, x1 - x0, y1 - y0, u0, v0, u1, v1, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x1, y0, x2 - x1, y1 - y0, u1, v0, u2, v1, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x2, y0, x3 - x2, y1 - y0, u2, v0, u3, v1, color.R, color.G, color.B, color.A, projection);

            DrawQuad(textureId, x0, y1, x1 - x0, y2 - y1, u0, v1, u1, v2, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x1, y1, x2 - x1, y2 - y1, u1, v1, u2, v2, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x2, y1, x3 - x2, y2 - y1, u2, v1, u3, v2, color.R, color.G, color.B, color.A, projection);

            DrawQuad(textureId, x0, y2, x1 - x0, y3 - y2, u0, v2, u1, v3, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x1, y2, x2 - x1, y3 - y2, u1, v2, u2, v3, color.R, color.G, color.B, color.A, projection);
            DrawQuad(textureId, x2, y2, x3 - x2, y3 - y2, u2, v2, u3, v3, color.R, color.G, color.B, color.A, projection);


        }


        private void DrawQuad(int textureId,
                      float x, float y, float w, float h,
                      float u1, float v1, float u2, float v2,
                      float r, float g, float b, float a,
                      Matrix4 projection)
        {
            // Защита от нулевого или отрицательного размера
            if (w <= 0.001f || h <= 0.001f)
                return;

            // Создаём вершины именно для этого куска
            float[] vertices = CreateQuadVertices(x, y, w, h, u1, v1, u2, v2);

            UploadVertices(vertices);
            BindAndSetUniforms(textureId, r, g, b, a, projection);
            DrawElements();
        }
        // ====================== Приватные хелперы ======================

        private static float[] CreateQuadVertices(float x, float y, float w, float h,float u1, float v1, float u2, float v2)
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

            // Текстура
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            if (_texLoc != -1) GL.Uniform1(_texLoc, 0);

            // Цвет
            if (_colorLoc != -1)
                GL.Uniform4(_colorLoc, r, g, b, a);

            // Проекция
            if (_projectionLoc != -1)
                GL.UniformMatrix4(_projectionLoc, false, ref projection);
        }

        private static void DrawElements()
        {
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0); // хорошая практика
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }

        public void DrawGlow(int textureId, float x, float y, float width, float height,float r, float g, float b, float baseAlpha,float glowIntensity, Matrix4 projection,int steps = 5, float spread = 4f)
        {
            if (glowIntensity <= 0f) return;

            // Рисуем несколько слоёв от большого к маленькому
            for (int i = steps; i >= 1; i--)
            {
                float factor = i / (float)steps;
                float offset = spread * factor;
                float alpha = baseAlpha * glowIntensity * (0.12f / i);   // убывание альфы с расстоянием

                float glowW = width + offset * 2f;
                float glowH = height + offset * 2f;
                float glowX = x - offset;
                float glowY = y - offset;

                Draw(textureId, glowX, glowY, glowW, glowH,
                     0f, 0f, 1f, 1f,
                     r, g, b, alpha,
                     projection);
            }
        }

        public void DrawGlowRect(float x, float y, float width, float height, Color4 color,
                        float baseAlpha, Matrix4 projection, int steps = 5, float spread = 8f)
        {
            if (baseAlpha <= 0f) return;

            for (int i = steps; i >= 1; i--)
            {
                float factor = i / (float)steps;
                float offset = spread * factor;
                float alpha = baseAlpha * (0.14f / i);

                DrawRect(x - offset, y - offset, width + offset * 2, height + offset * 2,
                         new Color4(color.R, color.G, color.B, alpha), projection);
            }
        }
    }
}