using Gdk;
using System;
using System.Collections.Generic;
using System.Text;
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

            // === 1. Базовые метрики ===
            float avgNPS = (float)(totalEvents / duration);                    // фразы в секунду
            float avgCharDensity = (float)(totalChars / duration);
            float avgPhraseLen = (float)totalChars / totalEvents;

            // === 2. Gaps ===
            var gaps = new List<double>();
            for (int i = 1; i < totalEvents; i++)
                gaps.Add(map.Events[i].time - map.Events[i - 1].time);

            double avgGap = gaps.Count > 0 ? gaps.Average() : 0;
            double gapStdDev = gaps.Count > 1 ? Math.Sqrt(gaps.Select(g => Math.Pow(g - avgGap, 2)).Average()) : 0;
            int fastGapsCount = gaps.Count(g => g < 0.7);                     // очень быстрые ноты
            float fastGapsRatio = gaps.Count > 0 ? (float)fastGapsCount / gaps.Count : 0;

            // === 3. Peak density (самое важное!) ===
            float peakNPS1s = CalculatePeakNPS(map.Events, 1.0f);   // макс. фраз в 1 секунду
            float peakNPS2s = CalculatePeakNPS(map.Events, 2.0f);   // макс. фраз в 2 секунды

            // === 4. Max phrase length ===
            int maxPhraseLen = map.Events.Max(ev => ev.text.Length);

            // === 5. Strain (напряжение) ===
            double strain = 0;
            for (int i = 0; i < totalEvents - 1; i++)
            {
                float gap = (float)(map.Events[i + 1].time - map.Events[i].time);
                if (gap > 0)
                    strain += Math.Pow(map.Events[i].text.Length, 1.2) / gap;
            }
            float avgStrain = (float)(strain / (totalEvents - 1));

            // === 6. Нормализация (максимумы подобраны по реальным картам) ===
            float normAvgNPS = Math.Min(1f, avgNPS / 1.8f);
            float normPeakNPS = Math.Min(1f, peakNPS2s / 5.5f);        // 2s окно — самый важный
            float normMaxPhrase = Math.Min(1f, maxPhraseLen / 65f);
            float normFastRatio = Math.Min(1f, fastGapsRatio / 0.35f);
            float normGapStdDev = Math.Min(1f, (float)gapStdDev / 2.8f);
            float normAvgStrain = Math.Min(1f, avgStrain / 28f);
            float normAvgPhrase = Math.Min(1f, avgPhraseLen / 32f);

            // === 7. Веса (подобраны так, чтобы сложные карты получали 5.5–8+ ★) ===
            float starRating = 1.8f +                                          // базовый уровень
                normAvgNPS * 1.8f * 2f +                                  // средняя скорость
                normPeakNPS * 3.2f * 2f +                                  // ← САМЫЙ БОЛЬШОЙ ВЕС (пики)
                normMaxPhrase * 2.0f * 1.8f +                                // длинные фразы
                normFastRatio * 1.5f * 1.5f +
                normGapStdDev * 1.2f * 1.4f +
                normAvgStrain * 2.5f * 1.6f +                                // напряжение
                normAvgPhrase * 0.8f * 1.2f;                                 // средняя длина (меньше веса)

            return (float)Math.Round(starRating, 2);
        }

        // Вспомогательный метод (добавь в класс)
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

                // Читаем существующий JSON
                string json = File.ReadAllText(tappPath);
                var map = JsonSerializer.Deserialize<JsonMap>(json);
                if (map == null) continue;

                // Вычисляем новую сложность
                // Для этого нужно преобразовать JsonMap в MapData (или создать временный объект)
                var tempMapData = new MapData
                {
                    Events = map.events,
                    endTime = map.endTime
                };
                float newRating = DifficultyCalculator.CalculateStarRating(tempMapData);

                // Если force=true или значение изменилось, обновляем
                if (force || Math.Abs(map.StarRating - newRating) > 0.01f)
                {
                    map.StarRating = newRating;
                    string newJson = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tappPath, newJson);
                    Console.WriteLine($"Обновлена карта {Path.GetFileName(folder)}: {newRating}");
                }
            }
        }
    }
}
