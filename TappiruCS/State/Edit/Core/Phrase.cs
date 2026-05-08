using System.Collections.Generic;
using TappiruCS.GameLogic;
using TappiruCS.State.Edit.Core;
using TappiruCS.UI;

namespace TappiruCS.State.Edit.Core
{
    public class Phrase : ITimelineSelectable
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Transcription { get; set; } = string.Empty;

        public List<TappiruCS.State.Edit.Core.SliderTiming> Sliders { get; set; } = new();

        public int[] mapping { get; set; } = new int[0];

        // Визуальные объекты
        public SpriteObject? VisualBar { get; set; }
        public List<SpriteObject> SliderVisuals { get; set; } = new();

        public Phrase(float startTime, float endTime, string text, string transcription)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
            Transcription = transcription;
        }

        public bool ContainsTime(float time) => time >= StartTime && time <= EndTime;

        // === Явная реализация интерфейса ===
        string ITimelineSelectable.GetDisplayName() => string.IsNullOrEmpty(Text) ? "Без текста" : Text;
        string ITimelineSelectable.GetTypeName() => "Фраза";
    }
}