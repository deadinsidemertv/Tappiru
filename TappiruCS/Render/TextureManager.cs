using OpenTK.Graphics.OpenGL4;
using TappiruCS.UI.API;
using static TappiruCS.UI.API.ContentPath;

namespace TappiruCS.Render
{
    public static class TextureManager
    {
        private static readonly Dictionary<string, int> _textures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);


        public static int GetTexture(string assetPath)
        {
            if (_textures.TryGetValue(assetPath, out int id))
                return id;

            string[] extensions = { ".png", ".tga", ".jpg", ".jpeg" };
            string fullPath = null;

            // 1. Прямой путь с подпапками из assetPath
            foreach (var ext in extensions)
            {
                string testPath = TEXTURES_ROOT.Combine(assetPath + ext);
                if (File.Exists(testPath))
                {
                    fullPath = testPath;
                    break;
                }
            }

            // 2. Если не нашли — ищем рекурсивно по всей папке Textures
            if (fullPath == null)
            {
                string fileName = Path.GetFileName(assetPath);
                var files = Directory.GetFiles(TEXTURES_ROOT, fileName + ".*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file);
                    if (extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        fullPath = file;
                        break;
                    }
                }
            }

            if (fullPath == null || !File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Текстура не найдена: {assetPath}");
                return 0;
            }

            id = TextureLoader.Load(fullPath);
            _textures[assetPath] = id;
            return id;
        }

        public static void UnloadAll()
        {
            foreach (var id in _textures.Values)
                GL.DeleteTexture(id);

            _textures.Clear();
            
        }
    }
}