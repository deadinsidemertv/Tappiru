using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace TappiruCS.Render
{
    public static class TextureLoader
    {
        public static int shaderProgram;
        public static int vao;
        public static int vbo;
        public static int ebo;

        public static int Load(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[TextureLoader] Ошибка: файл не найден {path}");
                return 0;
            }

            int textureID = GL.GenTexture();

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            byte[] pixelData = image.Data;

            // === СПЕЦИАЛЬНЫЙ ФИКС ДЛЯ ТВОЕГО ШРИФТА "main_*.tga" ===
            // У тебя белые буквы на чёрном фоне → альфа должна браться из яркости RGB
            bool isFontTexture = path.Contains("main_") || path.Contains("font");

            if (isFontTexture)
            {
                var newData = new byte[image.Width * image.Height * 4];

                for (int i = 0, j = 0; i < pixelData.Length; i += 4, j += 4)
                {
                    byte r = pixelData[i];
                    byte g = pixelData[i + 1];
                    byte b = pixelData[i + 2];
                    // byte a = pixelData[i + 3]; // этот альфа-канал у тебя почти всегда 255 или мусор

                    // Берём яркость (обычно зелёный канал самый чистый у BMFont)
                    byte alpha = (byte)((r + g + b) / 3);   // или просто g, если зелёный канал чистый

                    newData[j] = 255;   // R = белый
                    newData[j + 1] = 255;   // G = белый
                    newData[j + 2] = 255;   // B = белый
                    newData[j + 3] = alpha; // A = из яркости символа
                }
                pixelData = newData;
            }
            else
            {
                // Для обычных текстур (btn, black, menubg и т.д.) — нормальная обработка
                if (image.Comp != ColorComponents.RedGreenBlueAlpha)
                {
                    var newData = new byte[image.Width * image.Height * 4];
                    // ... (твой предыдущий код для обычных текстур)
                    for (int i = 0, j = 0; i < pixelData.Length; i += image.Comp == ColorComponents.RedGreenBlue ? 3 : 1, j += 4)
                    {
                        if (image.Comp == ColorComponents.RedGreenBlue)
                        {
                            newData[j] = pixelData[i];
                            newData[j + 1] = pixelData[i + 1];
                            newData[j + 2] = pixelData[i + 2];
                            newData[j + 3] = 255;
                        }
                        else // Grey
                        {
                            byte val = pixelData[i];
                            newData[j] = newData[j + 1] = newData[j + 2] = val;
                            newData[j + 3] = 255;
                        }
                    }
                    pixelData = newData;
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          image.Width, image.Height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            Console.WriteLine($"[TextureLoader] ✅ Загружена: {Path.GetFileName(path)} | ID={textureID} | {image.Width}x{image.Height}");

            return textureID;
        }

        

        public static void SetupGraphics()
        {
            // Vertex Shader
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

            // Fragment Shader
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

            int vertexShader = CompileShader(vertexShaderSource, ShaderType.VertexShader);
            int fragmentShader = CompileShader(fragmentShaderSource, ShaderType.FragmentShader);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Проверка линковки
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linkSuccess);
            if (linkSuccess == 0)
            {
                Console.WriteLine("Ошибка линковки шейдера: " + GL.GetProgramInfoLog(shaderProgram));
            }

            // Вершины (это можно вынести в SpriteBatch позже)
            float[] vertices = {
                -0.8f,  0.8f,  0.0f, 0.0f,
                 0.8f,  0.8f,  1.0f, 0.0f,
                 0.8f, -0.8f,  1.0f, 1.0f,
                -0.8f, -0.8f,  0.0f, 1.0f
            };

            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            // Blend включаем один раз здесь
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private static int CompileShader(string source, ShaderType type)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                Console.WriteLine($"Ошибка компиляции {type}: " + GL.GetShaderInfoLog(shader));
            }
            return shader;
        }

        public static int CreateTextureFromRawData(byte[] data, int width, int height)
        {
            int texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texId;
        }
    }
}