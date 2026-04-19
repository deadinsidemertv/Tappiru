using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace TappiruCS.Render
{
    /// <summary>
    /// Загрузчик текстур с кэшем, PBO-загрузкой и чистым разделением ответственности.
    /// </summary>
    public static class TextureLoader
    {
        // ── Публичные поля (нужны снаружи) ────────────────────────────────
        public static int shaderProgram;
        public static int vao, vbo, ebo;

        // ── Кэш: путь → ID текстуры ────────────────────────────────────────
        private static readonly Dictionary<string, int> _cache = new();

        // ── PBO: двойной буфер чтобы CPU и GPU не ждали друг друга ─────────
        private const int PBO_COUNT = 2;
        private const int PBO_MAX_SIZE = 64 * 1024 * 1024; // 64 МБ хватает для 4K RGBA
        private static readonly int[] _pbos = new int[PBO_COUNT];
        private static int _pboIndex = 0;        // чередуем 0 → 1 → 0 → ...
        private static bool _pbosReady = false;

        // ══════════════════════════════════════════════════════════════════
        //  ПУБЛИЧНЫЙ API
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Загружает текстуру из файла. Повторные вызовы с тем же путём
        /// возвращают кэшированный ID без лишней работы.
        /// </summary>
        public static int Load(string path)
        {
            if (_cache.TryGetValue(path, out int cached))
                return cached;

            if (!File.Exists(path))
            {
                Console.WriteLine($"[TextureLoader] ❌ Файл не найден: {path}");
                return 0;
            }

            ImageResult image;
            using (var stream = File.OpenRead(path))
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            byte[] pixels = IsFontTexture(path)
                ? ProcessFontPixels(image.Data)
                : image.Data;

            int id = UploadTexture(pixels, image.Width, image.Height, generateMipmaps: true);

            _cache[path] = id;
            return id;
        }

        public static int CreateTextureFromRawDataAsync(
            byte[] data, int width, int height, bool generateMipmaps = false)
        {
            return UploadTexture(data, width, height, generateMipmaps);
        }

        public static void Unload(int textureId)
        {
            // Удаляем из кэша
            var key = _cache.FirstOrDefault(kv => kv.Value == textureId).Key;
            if (key != null) _cache.Remove(key);

            GL.DeleteTexture(textureId);
        }

        // ══════════════════════════════════════════════════════════════════
        //  SETUP
        // ══════════════════════════════════════════════════════════════════

        public static void SetupGraphics()
        {
            SetupShaders();
            SetupQuadGeometry();
            SetupPBOs();

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public static void InitializeAsyncTextureUpload()
        {
            // Оставлено для обратной совместимости — SetupGraphics уже вызывает SetupPBOs
            if (!_pbosReady) SetupPBOs();
        }

        // ══════════════════════════════════════════════════════════════════
        //  ВНУТРЕННЯЯ ЛОГИКА
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Загружает пиксели в GPU через PBO (быстрее прямой передачи).
        /// Двойной буфер (ping-pong) устраняет stall между CPU и GPU.
        /// </summary>
        private static int UploadTexture(byte[] data, int width, int height, bool generateMipmaps)
        {
            if (!_pbosReady) SetupPBOs();

            // — Выбираем следующий PBO в очереди —
            int pbo = _pbos[_pboIndex];
            _pboIndex = (_pboIndex + 1) % PBO_COUNT;

            // — Копируем данные CPU → PBO (без ожидания GPU) —
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pbo);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, data.Length, IntPtr.Zero, BufferUsageHint.StreamDraw); // orphaning
            GL.BufferSubData(BufferTarget.PixelUnpackBuffer, IntPtr.Zero, data.Length, data);

            // — Создаём текстуру и загружаем из PBO —
            int texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);

            GL.TexImage2D(
                TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte,
                IntPtr.Zero   // ← IntPtr.Zero = читать из PBO, а не из RAM
            );

            // — Параметры фильтрации —
            var minFilter = generateMipmaps
                ? TextureMinFilter.LinearMipmapLinear
                : TextureMinFilter.Linear;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            if (generateMipmaps)
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            // — Отвязываем всё —
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texId;
        }

        private static void SetupPBOs()
        {
            for (int i = 0; i < PBO_COUNT; i++)
            {
                _pbos[i] = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbos[i]);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, PBO_MAX_SIZE, IntPtr.Zero, BufferUsageHint.StreamDraw);
            }
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            _pbosReady = true;

            
        }

        // ── Обработка пикселей ─────────────────────────────────────────────

        private static bool IsFontTexture(string path) =>
            path.Contains("main_") || path.Contains("font");

        /// <summary>
        /// Шрифты BMFont: белые буквы на чёрном → переводим яркость в альфа-канал.
        /// </summary>
        private static byte[] ProcessFontPixels(byte[] src)
        {
            var dst = new byte[src.Length];
            for (int i = 0; i < src.Length; i += 4)
            {
                byte alpha = (byte)((src[i] + src[i + 1] + src[i + 2]) / 3);
                dst[i] = 255;
                dst[i + 1] = 255;
                dst[i + 2] = 255;
                dst[i + 3] = alpha;
            }
            return dst;
        }

        // ── Шейдеры и геометрия ────────────────────────────────────────────

        private static void SetupShaders()
        {
            const string vert = @"
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

            const string frag = @"
                #version 330 core
                in vec2 TexCoord;
                out vec4 FragColor;
                uniform sampler2D tex;
                uniform vec4 color;
                void main()
                {
                    FragColor = texture(tex, TexCoord) * color;
                }";

            int vs = CompileShader(vert, ShaderType.VertexShader);
            int fs = CompileShader(frag, ShaderType.FragmentShader);

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vs);
            GL.AttachShader(shaderProgram, fs);
            GL.LinkProgram(shaderProgram);
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0)
                Console.WriteLine("[TextureLoader] ❌ Ошибка линковки: " + GL.GetProgramInfoLog(shaderProgram));

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        private static void SetupQuadGeometry()
        {
            float[] vertices = {
                -0.8f,  0.8f,  0f, 0f,
                 0.8f,  0.8f,  1f, 0f,
                 0.8f, -0.8f,  1f, 1f,
                -0.8f, -0.8f,  0f, 1f,
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
        }

        private static int CompileShader(string source, ShaderType type)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
                Console.WriteLine($"[TextureLoader] ❌ Ошибка {type}: " + GL.GetShaderInfoLog(shader));
            return shader;
        }
    }
}