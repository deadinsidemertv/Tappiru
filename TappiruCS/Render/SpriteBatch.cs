using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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

            // Атрибуты: Position (2) + UV (2)
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
    }
}