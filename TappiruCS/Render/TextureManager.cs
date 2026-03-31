using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace TappiruCS.Render
{
    public static class TextureManager   // можно сделать не static, если хочешь
    {
        private static readonly Dictionary<string, int> _textures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Основной метод — загружаем по имени
        public static int GetTexture(string assetName)
        {
            if (_textures.TryGetValue(assetName, out int id))
                return id;

            // Если ещё не загружена — загружаем
            string path = $"Textures/{assetName}.jpg";     // или .png

            if (!System.IO.File.Exists(path))
                path = $"Textures/{assetName}.png";         // пробуем png

            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"[ERROR] Текстура не найдена: {assetName}");
                return 0;
            }

            id = TextureLoader.Load(path);   // твой текущий загрузчик
            _textures[assetName] = id;

            Console.WriteLine($"[TextureManager] Загружена: {assetName} (ID={id})");
            return id;
        }

        // Очистка при выходе из игры
        public static void UnloadAll()
        {
            foreach (var id in _textures.Values)
                GL.DeleteTexture(id);

            _textures.Clear();
        }
    }
}