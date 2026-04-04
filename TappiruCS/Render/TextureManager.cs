using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.IO;

namespace TappiruCS.Render
{
    public static class TextureManager
    {
        private static readonly Dictionary<string, int> _textures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Загружает текстуру по имени актива (без расширения).
        /// Поддерживает подпапки! Пример: "Font/MyFont_0"
        /// </summary>
        public static int GetTexture(string assetPath)
        {
            if (_textures.TryGetValue(assetPath, out int id))
                return id;

            // Порядок расширений (TGA у тебя в приоритете)
            string[] extensions = { ".png", ".tga", ".jpg", ".jpeg" };

            string fullPath = null;

            // 1. Пробуем как есть (с подпапкой)
            foreach (var ext in extensions)
            {
                string testPath = $"Textures/{assetPath}{ext}";
                if (File.Exists(testPath))
                {
                    fullPath = testPath;
                    break;
                }
            }

            // 2. Если не нашли — пробуем без подпапки (на всякий случай)
            if (fullPath == null)
            {
                string assetName = Path.GetFileName(assetPath);
                foreach (var ext in extensions)
                {
                    string testPath = $"Textures/{assetName}{ext}";
                    if (File.Exists(testPath))
                    {
                        fullPath = testPath;
                        break;
                    }
                }
            }

            if (fullPath == null || !File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Текстура не найдена: {assetPath} (проверяли .png/.tga/.jpg/.jpeg в Textures/ и подпапках)");
                return 0;
            }

            id = TextureLoader.Load(fullPath);
            _textures[assetPath] = id;

            Console.WriteLine($"[TextureManager] ✅ Загружена: {assetPath} (ID={id}) | файл: {Path.GetFileName(fullPath)}");
            return id;
        }

        public static void UnloadAll()
        {
            foreach (var id in _textures.Values)
                GL.DeleteTexture(id);

            _textures.Clear();
            Console.WriteLine("[TextureManager] Все текстуры выгружены.");
        }
    }
}