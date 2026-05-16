// ProjectIO.cs — сохранение и загрузка проекта (.tappz / .tapp)
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using TappiruCS.GameLogic;

namespace TappiruCS.State.Edit.SaveLoad
{
    /// <summary>
    /// Инкапсулирует всю работу с файловой системой:
    /// загрузку архива, чтение/запись JSON, извлечение ассетов.
    /// </summary>
    internal class ProjectIO
    {
        // ── Публичные данные после загрузки ─────────────────────────────────────
        public string TappzPath { get; private set; } = string.Empty;
        public string ProjectDir { get; private set; } = string.Empty;
        public string Mp3Path { get; private set; } = string.Empty;
        public string BgPath { get; private set; } = string.Empty;

        // ── Открытие / распаковка ────────────────────────────────────────────────
        public JsonMap? Open(string tappzPath)
        {
            TappzPath = tappzPath;
            ProjectDir = BuildProjectDir(tappzPath);

            Directory.CreateDirectory(ProjectDir);

            if (File.Exists(tappzPath))
                ZipFile.ExtractToDirectory(tappzPath, ProjectDir, overwriteFiles: true);

            Mp3Path = Directory.GetFiles(ProjectDir, "*.mp3").FirstOrDefault() ?? string.Empty;
            BgPath = Directory.GetFiles(ProjectDir, "*.png")
                               .Concat(Directory.GetFiles(ProjectDir, "*.jpg"))
                               .FirstOrDefault() ?? string.Empty;

            return ReadMapJson(FindTappFile(ProjectDir));
        }

        // ── Сохранение ───────────────────────────────────────────────────────────
        public void Save(JsonMap map)
        {
            if (string.IsNullOrEmpty(TappzPath) || string.IsNullOrEmpty(ProjectDir))
                throw new InvalidOperationException("Проект не открыт — нечего сохранять.");

            string dataPath = FindTappFile(ProjectDir);

            string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dataPath, json);

            RewriteEntryInArchive(TappzPath, dataPath, json);

            Console.WriteLine("[ProjectIO] Проект сохранён.");
        }

        // ── Очистка временной папки ──────────────────────────────────────────────
        public void Cleanup()
        {
            if (string.IsNullOrEmpty(ProjectDir) || !Directory.Exists(ProjectDir)) return;
            try
            {
                Directory.Delete(ProjectDir, recursive: true);
                Console.WriteLine($"[ProjectIO] Временная папка удалена: {ProjectDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectIO] Не удалось удалить {ProjectDir}: {ex.Message}");
            }
        }

        // ── Вспомогательные ─────────────────────────────────────────────────────
        private static string BuildProjectDir(string tappzPath)
        {
            string name = Path.GetFileNameWithoutExtension(tappzPath);
            return Path.Combine(Directory.GetCurrentDirectory(), "Edit", name);
        }

        private static string FindTappFile(string dir)
        {
            string[] files = Directory.GetFiles(dir, "*.tapp", SearchOption.TopDirectoryOnly);
            return files.Length > 0 ? files[0] : Path.Combine(dir, "data.tapp");
        }

        private static JsonMap? ReadMapJson(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return new JsonMap();
                return JsonSerializer.Deserialize<JsonMap>(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectIO] Ошибка чтения .tapp: {ex.Message}");
                return new JsonMap();
            }
        }

        private static void RewriteEntryInArchive(string archivePath, string localFilePath, string json)
        {
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);

            // Удаляем старый .tapp
            var old = archive.Entries.FirstOrDefault(
                e => e.FullName.EndsWith(".tapp", StringComparison.OrdinalIgnoreCase));
            old?.Delete();

            // Пишем актуальный
            var entry = archive.CreateEntry(Path.GetFileName(localFilePath));
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes(json));
        }

        public void PublishMap(string songsRoot = "Songs", bool overwrite = true)
        {
            if (string.IsNullOrEmpty(ProjectDir) || !Directory.Exists(ProjectDir))
                throw new InvalidOperationException("Проект не открыт — нечего копировать.");

            string projectName = Path.GetFileNameWithoutExtension(TappzPath)
                                 ?? Path.GetFileName(ProjectDir);

            string targetDir = Path.Combine(songsRoot, projectName);

            Directory.CreateDirectory(songsRoot);
            CopyDirectory(ProjectDir, targetDir, overwrite);

            Console.WriteLine($"[ProjectIO] Проект скопирован в Songs/{projectName}");
        }

        private static void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir, overwrite);
            }
        }
    }
}