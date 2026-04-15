using System;
using System.Collections.Generic;
using System.Linq;

namespace TappiruCS.GameLogic
{
    public static class DifficultyCalculator
    {
        public static float CalculateStarRating(MapData map)
        {
            if (map?.Events == null || map.Events.Count == 0)
                return 1.0f;

            var events = map.Events;
            double duration = map.endTime - (events[0]?.startTime ?? 0);
            if (duration < 10) duration = 10;

            int totalEvents = events.Count;
            int totalSliders = events.Sum(e => e.sliders?.Count ?? 0);

            float avgNPS = (float)(totalEvents / duration);
            float peakNPS2s = CalculatePeakNPS(events, 2.0f);

            float sliderRatio = totalEvents > 0 ? (float)totalSliders / totalEvents : 0f;

            // === Сложность фразы: длина + время на экране ===
            double totalPhraseScore = 0;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                float timeOnScreen = (i + 1 < events.Count)
                    ? events[i + 1].startTime - ev.startTime
                    : (float)(map.endTime - ev.startTime);

                int length = ev.text.Length;

                float baseDifficulty = length / Math.Max(timeOnScreen, 0.25f);

                // Сильный штраф, если мало времени
                if (timeOnScreen < 0.6f)
                    baseDifficulty *= 2.1f;
                else if (timeOnScreen < 1.0f)
                    baseDifficulty *= 1.45f;
                else if (timeOnScreen < 1.8f)
                    baseDifficulty *= 1.1f;

                totalPhraseScore += baseDifficulty;
            }

            float avgPhraseDifficulty = (float)(totalPhraseScore / totalEvents);

            // Быстрые смены
            var gaps = new List<float>();
            for (int i = 1; i < events.Count; i++)
                gaps.Add(events[i].startTime - events[i - 1].startTime);

            float veryFastRatio = gaps.Count(g => g < 0.45f) / (float)Math.Max(1, gaps.Count);

            // ==================== НОРМАЛИЗАЦИЯ (растянутая) ====================

            float normPeakNPS = Math.Min(1.6f, peakNPS2s / 10.5f);      // растянули
            float normVeryFast = Math.Min(1.5f, veryFastRatio / 0.28f);
            float normSlider = Math.Min(1.4f, sliderRatio * 3.0f);
            float normPhrase = Math.Min(2.2f, avgPhraseDifficulty / 14.5f);   // главный фактор, растянут

            // ==================== ФИНАЛЬНАЯ ФОРМУЛА ====================

            float starRating =
                normPeakNPS * 3.8f +
                normVeryFast * 3.3f +
                normSlider * 2.7f +
                normPhrase * 4.1f;        // основной вес

            // Бонус за длину карты
            float lengthBonus = (float)Math.Min(0.7f, (duration - 80) / 160f);

            starRating += lengthBonus;
            starRating *= 1.08f;               // повысили общий уровень рейтингов

            return (float)Math.Round(Math.Max(1.5f, starRating), 2);
        }
        private static float CalculatePeakNPS(List<TimingEvent> events, float windowSeconds)
            {
                if (events.Count == 0) return 0f;
                int maxCount = 0;
                int j = 0;
                for (int i = 0; i < events.Count; i++)
                {
                    while (j < events.Count && events[j].startTime - events[i].startTime <= windowSeconds)
                        j++;
                    maxCount = Math.Max(maxCount, j - i);
                }
                return maxCount;
            }

            // ====================== Пересчёт всех карт ======================
            public static void RecalculateAllStarRatings(bool force = false)
        {
            string songsDir = Path.Combine(Directory.GetCurrentDirectory(), "Songs");
            if (!Directory.Exists(songsDir)) return;

            foreach (var folder in Directory.GetDirectories(songsDir))
            {
                var tappFiles = Directory.GetFiles(folder, "*.tapp");
                if (tappFiles.Length == 0) continue;

                string tappPath = tappFiles[0];
                string json = File.ReadAllText(tappPath);
                var map = System.Text.Json.JsonSerializer.Deserialize<JsonMap>(json);
                if (map == null) continue;

                var tempMapData = new MapData
                {
                    Events = map.events,
                    endTime = map.endTime
                };

                float newRating = CalculateStarRating(tempMapData);

                if (force || Math.Abs(map.StarRating - newRating) > 0.05f)
                {
                    map.StarRating = newRating;
                    string newJson = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tappPath, newJson);
                    Console.WriteLine($"[Difficulty] {Path.GetFileName(folder)} → {newRating}★");
                }
            }
        }
    }
}