using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TappiruCS.GameLogic;


namespace TappiruCS.Core
{
    public static class ScoreManager
    {
        private static readonly string ScoresPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TappiruCS", "scores.json");

        private static List<PlayerScore> _scores = new();

        static ScoreManager()
        {
            // Создаём папку, если её нет
            string? dir = Path.GetDirectoryName(ScoresPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            Load();
        }

        private static void Load()
        {
            if (!File.Exists(ScoresPath))
            {
                _scores = new List<PlayerScore>();
                return;
            }
            string json = File.ReadAllText(ScoresPath);
            _scores = JsonSerializer.Deserialize<List<PlayerScore>>(json) ?? new List<PlayerScore>();
        }

        private static void Save()
        {
            string json = JsonSerializer.Serialize(_scores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ScoresPath, json);
        }

        public static void AddScore(PlayerScore score)
        {
            if (score == null) return;
            _scores.Add(score);
            Save();
        }

        public static List<PlayerScore> GetScoresForMap(string mapHash)
        {
            return _scores.Where(s => s.MapHash == mapHash).OrderByDescending(s => s._score).ToList();
        }

        public static PlayerScore? GetBestScoreForMap(string mapHash)
        {
            return _scores.Where(s => s.MapHash == mapHash).OrderByDescending(s => s._score).FirstOrDefault();
        }
        public static List<PlayerScore> GetTopScoresForMap(string mapHash, int topCount = 10)
        {
            return _scores.Where(s => s.MapHash == mapHash)
                          .OrderByDescending(s => s._score)
                          .Take(topCount)
                          .ToList();
        }
    }
}
