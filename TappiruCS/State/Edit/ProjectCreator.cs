using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TappiruCS.GameLogic;   // предполагается, что JsonMap и TimingEvent находятся здесь

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Отвечает за создание новой папки проекта и файла .tapp
    /// </summary>
    internal class ProjectCreator
    {
        /// <summary>
        /// Создаёт новый проект и возвращает полный путь к файлу data.tapp
        /// </summary>
        /// <param name="title">Название карты (используется как имя папки)</param>
        /// <param name="mp3Path">Путь к выбранному MP3 файлу</param>
        /// <param name="bgPath">Путь к выбранному фоновому изображению</param>
        /// <returns>Полный путь к созданному .tapp файлу или null при ошибке</returns>
        public string? Create(string title, string mp3Path, string bgPath)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            try
            {
                string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Edit");
                Directory.CreateDirectory(baseDir);

                string safeName = SanitizeFileName(title);
                string tappzPath = Path.Combine(baseDir, safeName + ".tappz");

                // Имя JSON-файла внутри архива — делаем понятным
                string mapFileName = safeName + ".tapp";

                var jsonMap = CreateDefaultJsonMap(title);
                string json = JsonSerializer.Serialize(jsonMap, new JsonSerializerOptions { WriteIndented = true });

                // Создаём чистый ZIP без лишней папки
                using (var archive = ZipFile.Open(tappzPath, ZipArchiveMode.Create))
                {
                    // JSON данные
                    var mapEntry = archive.CreateEntry(mapFileName);
                    using (var stream = mapEntry.Open())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        writer.Write(json);

                    // MP3
                    string mp3Name = Path.GetFileName(mp3Path);
                    using (var fs = File.OpenRead(mp3Path))
                    using (var es = archive.CreateEntry(mp3Name).Open())
                        fs.CopyTo(es);

                    // Background
                    string bgExt = Path.GetExtension(bgPath).ToLowerInvariant();
                    string bgName = "bg" + bgExt;
                    using (var fs = File.OpenRead(bgPath))
                    using (var es = archive.CreateEntry(bgName).Open())
                        fs.CopyTo(es);
                }

                Console.WriteLine($"Проект успешно создан: {safeName}.tappz");
                return tappzPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания: {ex.Message}");
                return null;
            }
        }

        private JsonMap CreateDefaultJsonMap(string title)
        {
            return new JsonMap
            {
                title = title,
                artist = "",
                creator = "",
                previewTime = 0,
                endTime = 1,

                events = new List<TimingEvent>(),

                // Цвета по умолчанию (можно потом менять через слайдеры)
                tappedR = 0.4f,
                tappedG = 0.3f,
                tappedB = 0.6f,

                needR = 0.7f,
                needG = 0.3f,
                needB = 0.8f,

                completeR = 0.2f,
                completeG = 0.1f,
                completeB = 0.4f
            };
        }

        /// <summary>
        /// Очищает имя файла от недопустимых символов
        /// </summary>
        private string SanitizeFileName(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }
            return name.Trim();
        }
    }
}