using System;
using System.IO;
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
                string projectName = SanitizeFileName(title);
                string projectDir = Path.Combine(baseDir, projectName);

                // Создаём папку проекта
                Directory.CreateDirectory(projectDir);

                // Копируем MP3
                string mp3FileName = Path.GetFileName(mp3Path);
                string mp3Dest = Path.Combine(projectDir, mp3FileName);
                File.Copy(mp3Path, mp3Dest, true);

                // Копируем фон (сохраняем оригинальное расширение)
                string bgExtension = Path.GetExtension(bgPath);
                string bgDest = Path.Combine(projectDir, "bg" + bgExtension);
                File.Copy(bgPath, bgDest, true);

                // Создаём JSON карту
                var jsonMap = CreateDefaultJsonMap(title);

                string tappPath = Path.Combine(projectDir, "data.tapp");
                string json = JsonSerializer.Serialize(jsonMap, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(tappPath, json);

                Console.WriteLine($"Проект успешно создан: {projectDir}");
                return tappPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании проекта: {ex.Message}");
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
                StarRating = 0f,
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