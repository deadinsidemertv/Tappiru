using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TappiruCS.GameLogic
{
    public static class DifficultyCalculator
    {
        public static float CalculateStarRating(MapData map)
        {
            if (map == null || map.Events == null || map.Events.Count == 0)
                return 0f;

            int totalEvents = map.Events.Count;
            int totalChars = map.Events.Sum(ev => ev.text.Length);
            double duration = map.endTime - (map.Events[0]?.time ?? 0);
            if (duration <= 0) duration = 1;

            float avgNPS = (float)(totalEvents / duration);
            float avgPhraseLen = (float)totalChars / totalEvents;

            var gaps = new List<double>();
            for (int i = 1; i < totalEvents; i++)
                gaps.Add(map.Events[i].time - map.Events[i - 1].time);

            double avgGap = gaps.Count > 0 ? gaps.Average() : 0;
            double gapStdDev = gaps.Count > 1 ? Math.Sqrt(gaps.Select(g => Math.Pow(g - avgGap, 2)).Average()) : 0;
            float fastGapsRatio = gaps.Count > 0 ? (float)gaps.Count(g => g < 0.7) / gaps.Count : 0;

            float peakNPS2s = CalculatePeakNPS(map.Events, 2.0f);
            int maxPhraseLen = map.Events.Max(ev => ev.text.Length);

            double strain = 0;
            for (int i = 0; i < totalEvents - 1; i++)
            {
                float gap = (float)(map.Events[i + 1].time - map.Events[i].time);
                if (gap > 0)
                    strain += Math.Pow(map.Events[i].text.Length, 1.15) / gap;   // чуть смягчили
            }
            float avgStrain = (float)(strain / (totalEvents - 1));

            // === НОВАЯ НОРМАЛИЗАЦИЯ (ещё ниже) ===
            float normAvgNPS = Math.Min(1f, avgNPS / 1.6f);
            float normPeakNPS = Math.Min(1f, peakNPS2s / 7.0f);
            float normMaxPhrase = Math.Min(1f, maxPhraseLen / 90f);
            float normFastRatio = Math.Min(1f, fastGapsRatio / 0.45f);
            float normGapStdDev = Math.Min(1f, (float)gapStdDev / 3.2f);
            float normAvgStrain = Math.Min(1f, avgStrain / 35f);
            float normAvgPhrase = Math.Min(1f, avgPhraseLen / 40f);

            // === ВЕСА (понижены) ===
            float starRating =
                normAvgNPS * 2.0f * 2.0f +
                normPeakNPS * 4.2f * 2.2f +     // пики всё ещё главный фактор
                normMaxPhrase * 1.2f * 1.2f +
                normFastRatio * 1.6f * 1.4f +
                normGapStdDev * 1.0f * 1.3f +
                normAvgStrain * 2.8f * 1.6f +
                normAvgPhrase * 0.4f * 1.0f;

            starRating *= 0.68f;        // ← главный понижающий множитель

            return (float)Math.Round(starRating, 2);
        }

        private static float CalculatePeakNPS(List<TimingEvent> events, float windowSeconds)
        {
            if (events.Count == 0) return 0f;
            int maxCount = 0;
            int j = 0;
            for (int i = 0; i < events.Count; i++)
            {
                while (j < events.Count && events[j].time - events[i].time <= windowSeconds)
                    j++;
                maxCount = Math.Max(maxCount, j - i);
            }
            return maxCount;
        }

        public static void RecalculateAllStarRatings(bool force = false)
        {
            string songsDir = Path.Combine(Directory.GetCurrentDirectory(), "Songs");
            if (!Directory.Exists(songsDir)) return;

            var folders = Directory.GetDirectories(songsDir);
            foreach (var folder in folders)
            {
                var tappFiles = Directory.GetFiles(folder, "*.tapp");
                if (tappFiles.Length == 0) continue;
                string tappPath = tappFiles[0];

                string json = File.ReadAllText(tappPath);
                var map = JsonSerializer.Deserialize<JsonMap>(json);
                if (map == null) continue;

                var tempMapData = new MapData
                {
                    Events = map.events,
                    endTime = map.endTime
                };

                float newRating = DifficultyCalculator.CalculateStarRating(tempMapData);

                if (force || Math.Abs(map.StarRating - newRating) > 0.01f)
                {
                    map.StarRating = newRating;
                    string newJson = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tappPath, newJson);
                    Console.WriteLine($"Обновлена карта {Path.GetFileName(folder)} → {newRating}★");
                }
            }
        }
    }
}