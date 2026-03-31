using System;
using System.Collections.Generic;
using System.Text;
using StbImageSharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace TappiruCS.Render
{
    public static class TextureLoader
    {
        public static int fontTexture;
        

        public static int textureWidth, textureHeight;


        public static int shaderProgram;    // идентификатор шейдерной программы
        public static int vao;              // Vertex Array Object (хранит настройки вершин)
        public static int vbo;              // Vertex Buffer Object (хранит вершины)
        public static int ebo;              // Element Buffer Object (индексы для рисования)

        public static int Load(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Ошибка: файл текстуры не найден: {path}");
                return 0;
            }

            // Генерируем новый texture ID
            int textureID = GL.GenTexture();

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          image.Width, image.Height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

            // Параметры фильтрации (важно для качества)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Wrapping (чтобы не было артефактов по краям)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Console.WriteLine($"Текстура загружена: ID={textureID}, размер={image.Width}x{image.Height}");

            textureWidth = image.Width;
            textureHeight = image.Height;

            return textureID;   // ← возвращаем ID
        }

        public static void SetupGraphics()
        {
            string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec2 aPos;
            layout(location = 1) in vec2 aTexCoord;
            out vec2 TexCoord;
            uniform mat4 projection;

            void main()
            {
            gl_Position = projection * vec4(aPos, 0.0, 1.0);
            TexCoord = aTexCoord;
            }";

            string fragmentShaderSource = @"
            #version 330 core
            in vec2 TexCoord;
            out vec4 FragColor;
            uniform sampler2D tex;
            uniform vec4 color;

            void main()
            {
                FragColor = texture(tex, TexCoord) * color;                
            }";

            int CompileShader(string source, ShaderType type)   
            {
                int shader = GL.CreateShader(type);               // просим OpenGL создать объект шейдера
                GL.ShaderSource(shader, source);                  // передаём исходный код
                GL.CompileShader(shader);                         // компилируем

                // Проверка ошибок компиляции
                GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    string info = GL.GetShaderInfoLog(shader);
                    Console.WriteLine("Ошибка компиляции шейдера: " + info);
                }
                return shader;
            }

            // В методе SetupGraphics:
            int vertexShader = CompileShader(vertexShaderSource, ShaderType.VertexShader);
            int fragmentShader = CompileShader(fragmentShaderSource, ShaderType.FragmentShader);

            shaderProgram = GL.CreateProgram();                   // создаём пустую программу
            GL.AttachShader(shaderProgram, vertexShader);         // прикрепляем вершинный шейдер
            GL.AttachShader(shaderProgram, fragmentShader);       // прикрепляем фрагментный шейдер
            GL.LinkProgram(shaderProgram);                        // линкуем (связываем) шейдеры в программу

            // Проверка ошибок линковки
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linkSuccess);
            if (linkSuccess == 0)
            {
                string info = GL.GetProgramInfoLog(shaderProgram);
                Console.WriteLine("Ошибка линковки: " + info);
            }

            // Шейдеры больше не нужны после линковки, можно удалить
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            float[] vertices = {
                // позиции (x, y)   // текстурные координаты (u, v)
                -0.8f,  0.8f,       0.0f, 0.0f, // левый верхний
                 0.8f,  0.8f,       1.0f, 0.0f, // правый верхний
                 0.8f, -0.8f,       1.0f, 1.0f, // правый нижний
                -0.8f, -0.8f,       0.0f, 1.0f  // левый нижний
            };

            uint[] indices = {
                0, 1, 2,  // первый треугольник
                2, 3, 0   // второй треугольник
            };
            vao = GL.GenVertexArray();                            // создаём VAO
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();                                 // создаём VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);         // привязываем VBO как буфер вершин
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw); // загружаем данные

            ebo = GL.GenBuffer();                                 // создаём EBO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);  // привязываем EBO как индексный буфер
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw); // загружаем индексы

            // Указываем, как читать атрибуты вершин
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0); // атрибут 0 (позиция) — 2 float, шаг 4 float, смещение 0
            GL.EnableVertexAttribArray(0);                        // включаем атрибут 0

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float)); // атрибут 1 (текстурные координаты) — 2 float, шаг 4 float, смещение 2 float
            GL.EnableVertexAttribArray(1);                        // включаем атрибут 1

            GL.BindVertexArray(0);

        }

        

       



    }
}
