// ProjectCreator.cs — создание нового .tappz файла
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TappiruCS.GameLogic;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Создаёт новый архив проекта (.tappz): упаковывает JSON-карту, MP3 и фон.
    /// Не содержит UI-логики — чистая файловая операция.
    /// </summary>
    internal class ProjectCreator
    {
        /// <returns>Полный путь к созданному .tappz или <c>null</c> при ошибке.</returns>
        public string? Create(string title, string artist, string mp3Path, string bgPath)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            try
            {
                string safeTitle = SanitizeName(title);
                string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Edit");
                Directory.CreateDirectory(outputDir);

                string archivePath = Path.Combine(outputDir, safeTitle + ".tappz");
                string mapEntryName = safeTitle + ".tapp";

                var map = BuildDefaultMap(title, artist);
                string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });

                using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

                WriteText(archive, mapEntryName, json);
                CopyFile(archive, mp3Path, Path.GetFileName(mp3Path));
                CopyFile(archive, bgPath, "bg" + Path.GetExtension(bgPath).ToLowerInvariant());

                Console.WriteLine($"[ProjectCreator] Создан: {safeTitle}.tappz");
                return archivePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectCreator] Ошибка: {ex.Message}");
                return null;
            }
        }

        // ── Вспомогательные ─────────────────────────────────────────────────────
        private static JsonMap BuildDefaultMap(string title, string artist) => new JsonMap
        {
            title = title,
            artist = artist,
            creator = "",
            previewTime = 0,
            endTime = 1,
            events = new(),

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

        private static void WriteText(ZipArchive archive, string entryName, string text)
        {
            using var stream = archive.CreateEntry(entryName).Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(text);
        }

        private static void CopyFile(ZipArchive archive, string sourcePath, string entryName)
        {
            using var src = File.OpenRead(sourcePath);
            using var dest = archive.CreateEntry(entryName).Open();
            src.CopyTo(dest);
        }

        private static string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}