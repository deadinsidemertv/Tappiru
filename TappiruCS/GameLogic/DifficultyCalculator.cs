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
                    : (float)(map.endTime - ev.startTime + 1.0); // last phrase

                // Use transcription length instead of raw text length
                int transcriptionLength = ev.transription?.Length ?? ev.text?.Length ?? 0;
                int sliderCount = ev.sliders?.Count ?? 0;

                // Base difficulty for the phrase
                float reactionFactor = 1.0f / Math.Max(timeOnScreen, 0.2f);

                // Long transcription with little time is very hard
                float lengthFactor = (float)Math.Pow(transcriptionLength, 1.12);

                float phraseBase = lengthFactor * reactionFactor;

                // Heavy penalty for very short time windows
                if (timeOnScreen < 0.55f)
                    phraseBase *= 2.4f;
                else if (timeOnScreen < 0.85f)
                    phraseBase *= 1.55f;

                // Sliders add difficulty (especially if time is short)
                float sliderPenalty = sliderCount * (timeOnScreen < 1.2f ? 1.8f : 1.1f);

                totalFCDifficulty += phraseBase + sliderPenalty;
            }

            float avgFCDifficulty = (float)(totalFCDifficulty / totalEvents);

            // Peak density (heaviest 2 seconds)
            float peakNPS2s = CalculatePeakNPS(events, 2.0f);

            // Normalization (stretched scale)
            float normFC = Math.Min(2.4f, avgFCDifficulty / 13.5f);   // main factor
            float normPeak = Math.Min(1.6f, peakNPS2s / 11.0f);
            float normSliderRatio = Math.Min(1.4f, (float)totalSliders / totalEvents * 1.85f);

            float starRating =
                normFC * 4.3f +
                normPeak * 3.1f +
                normSliderRatio * 2.5f;

            // Length bonus for longer maps
            float lengthBonus = (float)Math.Min(0.8f, (duration - 90) / 170f);
            starRating += lengthBonus;

            starRating *= 1.12f;        // raise overall rating level

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
    }
}