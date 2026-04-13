// Phrase.cs
using System.Collections.Generic;
using TappiruCS.GameLogic;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    public class Phrase
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string Text { get; set; } = string.Empty;

        // Слайдеры внутри этой фразы
        public List<SliderTiming> Sliders { get; set; } = new();

        // Визуальные объекты
        public SpriteObject? VisualBar { get; set; }      // отрезок фразы на таймлайне
        public List<SpriteObject> SliderVisuals { get; set; } = new();

        public Phrase(float startTime, float endTime, string text)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
        }

        public bool ContainsTime(float time) => time >= StartTime && time <= EndTime;
    }
}