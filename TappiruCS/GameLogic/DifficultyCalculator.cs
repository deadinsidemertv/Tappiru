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

            double totalFCDifficulty = 0.0;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                float timeOnScreen = (i + 1 < events.Count)
                    ? events[i + 1].startTime - ev.startTime
                    : (float)(map.endTime - ev.startTime + 1.0); // последняя фраза

                int length = ev.text.Length;
                int sliderCount = ev.sliders?.Count ?? 0;

                // Базовая сложность фразы
                float reactionFactor = 1.0f / Math.Max(timeOnScreen, 0.2f);

                // Длинная фраза при малом времени — очень тяжело
                float lengthFactor = (float)Math.Pow(length, 1.12);

                float phraseBase = lengthFactor * reactionFactor;

                // Сильный штраф за очень мало времени
                if (timeOnScreen < 0.55f)
                    phraseBase *= 2.4f;
                else if (timeOnScreen < 0.85f)
                    phraseBase *= 1.55f;

                // Слайдеры добавляют сложность (особенно если времени мало)
                float sliderPenalty = sliderCount * (timeOnScreen < 1.2f ? 1.8f : 1.1f);

                totalFCDifficulty += phraseBase + sliderPenalty;
            }

            float avgFCDifficulty = (float)(totalFCDifficulty / totalEvents);

            // Peak плотность (самые тяжёлые 2 секунды)
            float peakNPS2s = CalculatePeakNPS(events, 2.0f);

            // Нормализация (растянутая шкала)
            float normFC = Math.Min(2.4f, avgFCDifficulty / 13.5f);   // главный фактор
            float normPeak = Math.Min(1.6f, peakNPS2s / 11.0f);
            float normSliderRatio = Math.Min(1.4f, (float)totalSliders / totalEvents * 3.2f);

            float starRating =
                normFC * 4.3f +
                normPeak * 3.1f +
                normSliderRatio * 2.5f;

            // Бонус за длинную карту
            float lengthBonus = (float)Math.Min(0.8f, (duration - 90) / 170f);
            starRating += lengthBonus;

            starRating *= 1.12f;        // повышаем общий уровень рейтингов

            return (float)Math.Round(Math.Max(1.8f, starRating), 2);
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