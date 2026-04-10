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
            MapData mapdata = new MapData();
            string[] bgP = Directory.GetFiles(mapFolderPath, "*.jpg");
            mapdata.backGroundPath = bgP[0];
            string[] audioP = Directory.GetFiles(mapFolderPath, "*.mp3");
            mapdata.audioPath = audioP[0];
            string[] dataP = Directory.GetFiles(mapFolderPath, "*.tapp");
            mapdata.dataPath = dataP[0];
            mapdata.Path = mapFolderPath;

            // === ВЫЧИСЛЕНИЕ ХЕША ФАЙЛА .tapp ===
            string tappFilePath = mapdata.dataPath;
            string computedHash = ComputeFileHash(tappFilePath);
            mapdata.MapHash = computedHash;
            

            string json = File.ReadAllText(tappFilePath);
            JsonMap tmp = JsonSerializer.Deserialize<JsonMap>(json);
            mapdata.Events = tmp.events;
            mapdata.endTime = tmp.endTime;
            mapdata.title = tmp.title;
            mapdata.creator = tmp.creator;
            mapdata.artist = tmp.artist;

            mapdata.tappedR = tmp.tappedR;
            mapdata.tappedG = tmp.tappedG;
            mapdata.tappedB = tmp.tappedB;
            mapdata.needR = tmp.needR;
            mapdata.needG = tmp.needG;
            mapdata.needB = tmp.needB;
            mapdata.completeR = tmp.completeR;
            mapdata.completeG = tmp.completeG;
            mapdata.completeB = tmp.completeB;
            mapdata.StarRating = tmp.StarRating;

            foreach (var ev in mapdata.Events)
                ev.text = ev.text.ToLowerInvariant();

            Console.WriteLine($"{mapdata.title} - Хеш: {mapdata.MapHash}");

            

            return mapdata;
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