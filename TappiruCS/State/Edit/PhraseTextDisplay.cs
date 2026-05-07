// PhraseTextDisplay.cs
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit
{
    public class PhraseTextDisplay
    {
        private readonly Scene _scene;

        private TextObject? _upperText;
        private readonly List<TextObject> _lowerChars = new();

        public Phrase? ActivePhrase { get; private set; }

        public event Action<Phrase, int>? OnSliderRequested;

        public PhraseTextDisplay(Scene scene)
        {
            _scene = scene;
        }

        public void Sync(Phrase? currentPhrase)
        {
 
            ActivePhrase = currentPhrase;
            Rebuild();
        }

        private void Rebuild()
        {
            Clear();

            if (ActivePhrase == null) return;

            RebuildUpperText();
            RebuildLowerInteractiveText();
        }

        private void RebuildUpperText()
        {
            if (string.IsNullOrEmpty(ActivePhrase!.Text)) return;

            _upperText = new TextObject(ActivePhrase.Text, 960, 360, 68f)
            {
                Color = new Color4(0.85f, 0.88f, 0.95f, 0.9f),
                Align = TextAlign.Center,
                ScaleMultiply = 1f,
                Layer = 5,
                AllowHover = false,
                FixedColor = true
            };

            _scene.Add(_upperText);
        }

        private void RebuildLowerInteractiveText()
        {
            string displayText = !string.IsNullOrEmpty(ActivePhrase!.Transcription)
                ? ActivePhrase.Transcription
                : ActivePhrase.Text;

            if (string.IsNullOrEmpty(displayText)) return;

            const float baseFontSize = 64f;
            const float desiredTracking = 14f;

            var font = FontManager.Get("default") ?? FontManager._defaultFont;
            if (font == null) return;

            float scale = font.GetScaleFromFontSize(baseFontSize);

            List<float> charAdvances = new(displayText.Length);
            float totalWidth = 0f;
            char prev = '\0';

            for (int i = 0; i < displayText.Length; i++)
            {
                char c = displayText[i];
                if (font.TryGetRenderedGlyph(c, out var glyph) && glyph != null)
                {
                    float kerning = prev != '\0' ? font.GetKerning(prev, c) : 0f;
                    float advance = kerning + glyph.Info.XAdvance;

                    charAdvances.Add(advance * scale + desiredTracking);
                    totalWidth += advance * scale;
                }
                else
                {
                    charAdvances.Add(baseFontSize * 0.75f + desiredTracking);
                    totalWidth += baseFontSize * 0.75f;
                }
                prev = c;
            }

            float startX = 760f - (totalWidth + desiredTracking) / 2f;
            float currentX = startX;

            for (int i = 0; i < displayText.Length; i++)
            {
                // === ИСПРАВЛЕНИЕ 2: проверяем, есть ли уже слайдер для этой буквы ===
                bool hasSlider = ActivePhrase.Sliders.Any(s => s.charIndex == i);

                var charObj = new TextObject(displayText[i].ToString(), currentX, 475, baseFontSize)
                {
                    AllowHover = !hasSlider,                    // ← нельзя навести, если уже есть слайдер
                    FixedColor = hasSlider,
                    Color = hasSlider
                            ? new Color4(1f, 0.35f, 0.35f, 1f)
                            : Color4.White,
                    Align = TextAlign.Left,
                    ScaleMultiply = 1f,
                    Layer = 5
                };

                // === ИСПРАВЛЕНИЕ 1: клик только если нет слайдера ===
                if (!hasSlider)
                {
                    int idx = i;
                    charObj.OnClick = _ => OnSliderRequested?.Invoke(ActivePhrase!, idx);
                }

                _scene.Add(charObj);
                _lowerChars.Add(charObj);

                currentX += charAdvances[i];
            }
        }

        public void Clear()
        {
            if (_upperText != null)
            {
                _scene.Remove(_upperText);
                _upperText = null;
            }

            foreach (var ch in _lowerChars)
                _scene.Remove(ch);

            _lowerChars.Clear();
        }

        public void Dispose() => Clear();
    }
}