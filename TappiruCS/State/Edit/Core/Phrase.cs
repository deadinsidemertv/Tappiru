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

            Mapping = new List<int>();
            ResizeMappingTo(text.Length);
        }

        public bool ContainsTime(float time) => time >= StartTime && time <= EndTime;

        public void ResizeMappingTo(int targetLength)
        {
            if (targetLength < 0) targetLength = 0;

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
            ResizeMappingTo(Text.Length); // гарантируем правильную длину

            for (int i = 0; i < Text.Length; i++)
            {
                char ch = Text[i];

                // ←←← ИСПРАВЛЕНИЕ: применяем дефолт ТОЛЬКО если значение ещё не задано (0)
                if (Mapping[i] == 0)
                {
                    if (IsJapaneseKana(ch))
                    {
                        Mapping[i] = defaultKanaLengths.TryGetValue(ch, out int val) ? val : 1;
                    }
                    else
                    {
                        Mapping[i] = 0;
                    }
                }
            }
        }
        public bool IsJapaneseKana(char c)
        {
            return (c >= 0x3040 && c <= 0x309F) || // Hiragana
                   (c >= 0x30A0 && c <= 0x30FF);   // Katakana
        }
        // === Явная реализация интерфейса ===
        string ITimelineSelectable.GetDisplayName() => string.IsNullOrEmpty(Text) ? "Без текста" : Text;
        string ITimelineSelectable.GetTypeName() => "Фраза";
    }
}