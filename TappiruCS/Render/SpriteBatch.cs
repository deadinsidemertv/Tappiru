using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.Render
{
    public class SpriteBatch
    {
        private int shaderProgram;
        private int vao;
        private int vbo;
        private int ebo;
        float[] vertices;
        private int projectionLoc;
        Matrix4 projection;
        public SpriteBatch(int shaderProgram)
        {
            this.shaderProgram = shaderProgram;
            projectionLoc = GL.GetUniformLocation(shaderProgram, "projection");

            // Генерируем VAO
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // Генерируем VBO (пока без данных)
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            // Данные вершин будут загружаться позже в Draw

            // Генерируем EBO и загружаем индексы (они не меняются)
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Настраиваем атрибуты вершин (позиция и текстурные координаты)
            // Позиция: 2 float, смещение 0, шаг 4 float (позиция + uv)
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // UV: 2 float, смещение 2 float, шаг 4 float
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Отвязываем VAO
            GL.BindVertexArray(0);
        }

        public void Draw(int texture, float x, float y, float width, float height,
                 float u1, float v1, float u2, float v2,
                 float r, float g, float b, float a,
                 Matrix4 projection)
        {
            // Вершины для пиксельных координат (левая верхняя точка)
            float[] vertices = {
        x,     y,           u1, v1,
        x+width, y,         u2, v1,
        x+width, y+height,  u2, v2,
        x,     y+height,    u1, v2
    };

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            GL.UseProgram(shaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            int texLoc = GL.GetUniformLocation(shaderProgram, "tex");
            GL.Uniform1(texLoc, 0);
            int colorLoc = GL.GetUniformLocation(shaderProgram, "color");
            if (colorLoc != -1)
                GL.Uniform4(colorLoc, r, g, b, a);

            // Устанавливаем проекцию
            int projLoc = GL.GetUniformLocation(shaderProgram, "projection");
            if (projLoc != -1)
                GL.UniformMatrix4(projLoc, false, ref projection);


            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }


    }
}
