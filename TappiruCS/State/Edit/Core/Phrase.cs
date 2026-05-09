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

        public List<int> Mapping { get; set; } = new List<int>();

        // Визуальные объекты
        public SpriteObject? VisualBar { get; set; }
        public List<SpriteObject> SliderVisuals { get; set; } = new();

        public Phrase(float startTime, float endTime, string text, string transcription)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
            Transcription = transcription;
            Mapping = new List<int>(new int[text.Length]);
        }

        public bool ContainsTime(float time) => time >= StartTime && time <= EndTime;

        public void ResizeMappingTo(int targetLength)
        {
            if (Mapping.Count == targetLength) return;

            if (Mapping.Count < targetLength)
            {
                while (Mapping.Count < targetLength)
                    Mapping.Add(0);
            }
            else
            {
                Mapping.RemoveRange(targetLength, Mapping.Count - targetLength);
            }
        }
        public void ApplyDefaultMapping(Dictionary<char, int> defaultKanaLengths)
        {
            if (Mapping == null || Mapping.Count != Text.Length)
                ResizeMappingTo(Text.Length);

            for (int i = 0; i < Text.Length; i++)
            {
                char ch = Text[i];

                if (IsJapaneseKana(ch)) // только кана, не кандзи
                {
                    if (Mapping[i] == 0) // если ещё не задано
                    {
                        Mapping[i] = defaultKanaLengths.TryGetValue(ch, out int val) ? val : 1;
                    }
                }
                else
                {
                    Mapping[i] = 0; // пунктуация и кандзи = 0
                }
            }
        }
        private bool IsJapaneseKana(char c)
        {
            return (c >= 0x3040 && c <= 0x309F) || // Hiragana
                   (c >= 0x30A0 && c <= 0x30FF);   // Katakana
        }
        // === Явная реализация интерфейса ===
        string ITimelineSelectable.GetDisplayName() => string.IsNullOrEmpty(Text) ? "Без текста" : Text;
        string ITimelineSelectable.GetTypeName() => "Фраза";
    }
}