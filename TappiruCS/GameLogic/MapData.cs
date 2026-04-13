// MapData.cs
using System.Collections.Generic;

namespace TappiruCS.GameLogic
{
    public class JsonMap
    {
        public string MapHash { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string artist { get; set; } = string.Empty;
        public string creator { get; set; } = string.Empty;

        public double previewTime { get; set; } = 0;
        public double endTime { get; set; } = 0;

        public float StarRating { get; set; } = 0f;

        // Цвета
        public float tappedR { get; set; } = 0.4f;
        public float tappedG { get; set; } = 0.3f;
        public float tappedB { get; set; } = 0.6f;

        public float needR { get; set; } = 0.7f;
        public float needG { get; set; } = 0.3f;
        public float needB { get; set; } = 0.8f;

        public float completeR { get; set; } = 0.2f;
        public float completeG { get; set; } = 0.1f;
        public float completeB { get; set; } = 0.4f;

        // События (фразы)
        public List<TimingEvent> events { get; set; } = new List<TimingEvent>();

        // Глобальные настройки слайдеров
        public double GlobalSliderPerfectStartWindow { get; set; } = 0.10;
        public double GlobalSliderGoodStartWindow { get; set; } = 0.35;
        public double GlobalSliderPerfectEndWindow { get; set; } = 0.15;
        public double GlobalSliderGoodEndWindow { get; set; } = 0.45;

        public double SliderApproachTime { get; set; } = 1.2;
    }

    // Для совместимости с загрузчиком (если нужно)
    public class MapData
    {
        public string MapHash { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string artist { get; set; } = string.Empty;
        public string creator { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
        public string audioPath { get; set; } = string.Empty;
        public string backGroundPath { get; set; } = string.Empty;
        public string dataPath { get; set; } = string.Empty;

        public float StarRating { get; set; }

        public double previewTime { get; set; }
        public double endTime { get; set; }

        public float tappedR { get; set; }
        public float tappedG { get; set; }
        public float tappedB { get; set; }

        public float needR { get; set; }
        public float needG { get; set; }
        public float needB { get; set; }

        public float completeR { get; set; }
        public float completeG { get; set; }
        public float completeB { get; set; }

        public List<TimingEvent> Events { get; set; } = new List<TimingEvent>();

        public double GlobalSliderPerfectStartWindow { get; set; } = 0.10;
        public double GlobalSliderGoodStartWindow { get; set; } = 0.35;
        public double GlobalSliderPerfectEndWindow { get; set; } = 0.15;
        public double GlobalSliderGoodEndWindow { get; set; } = 0.45;
        public double SliderApproachTime { get; set; } = 1.2;
    }
}