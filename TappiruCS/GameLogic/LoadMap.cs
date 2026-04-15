using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace TappiruCS.GameLogic
{
    public static class LoadMap
    {
        public static MapData MapLoad(string mapFolderPath)
        {
            Console.WriteLine($"[MapLoad] Загрузка папки: {mapFolderPath}");

            MapData mapdata = new MapData();
            mapdata.Path = mapFolderPath;

            try
            {
                // Background
                var bgP = Directory.GetFiles(mapFolderPath, "*.jpg");
                if (bgP.Length > 0)
                {
                    mapdata.backGroundPath = bgP[0];
                    Console.WriteLine($"[MapLoad] BG найден: {Path.GetFileName(bgP[0])}");
                }
                else
                {
                    Console.WriteLine("[MapLoad] WARNING: .jpg файл не найден!");
                    // можно поставить дефолтный фон, если хочешь
                }

                // Audio
                var audioP = Directory.GetFiles(mapFolderPath, "*.mp3");
                if (audioP.Length > 0)
                {
                    mapdata.audioPath = audioP[0];
                    Console.WriteLine($"[MapLoad] MP3 найден: {Path.GetFileName(audioP[0])}");
                }
                else
                    Console.WriteLine("[MapLoad] WARNING: .mp3 файл не найден!");

                // Tapp file
                var dataP = Directory.GetFiles(mapFolderPath, "*.tapp");
                if (dataP.Length == 0)
                    throw new Exception("Не найден .tapp файл в папке!");

                string tappFilePath = dataP[0];
                mapdata.dataPath = tappFilePath;
                Console.WriteLine($"[MapLoad] TAPP файл: {Path.GetFileName(tappFilePath)}");

                // Читаем JSON
                string json = File.ReadAllText(tappFilePath, System.Text.Encoding.UTF8);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
                JsonMap tmp = JsonSerializer.Deserialize<JsonMap>(json, options);

                if (tmp == null)
                    throw new Exception("Не удалось десериализовать JSON");

                // Копируем данные
                mapdata.MapHash = ComputeFileHash(tappFilePath);
                mapdata.title = tmp.title ?? "Unknown";
                mapdata.artist = tmp.artist ?? "";
                mapdata.creator = tmp.creator ?? "";
                mapdata.endTime = tmp.endTime;
                mapdata.StarRating = tmp.StarRating;

                mapdata.tappedR = tmp.tappedR; mapdata.tappedG = tmp.tappedG; mapdata.tappedB = tmp.tappedB;
                mapdata.needR = tmp.needR; mapdata.needG = tmp.needG; mapdata.needB = tmp.needB;
                mapdata.completeR = tmp.completeR; mapdata.completeG = tmp.completeG; mapdata.completeB = tmp.completeB;

                mapdata.Events = tmp.events ?? new List<TimingEvent>();

                foreach (var ev in mapdata.Events)
                    if (ev?.text != null) ev.text = ev.text.ToLowerInvariant();

                Console.WriteLine($"[MapLoad] УСПЕШНО загружена: {mapdata.title}");
                return mapdata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapLoad] ОШИБКА: {ex.Message}");
                throw;
            }
        }

        // Добавьте этот вспомогательный метод прямо в класс LoadMap
        private static string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}