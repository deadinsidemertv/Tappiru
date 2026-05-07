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

        private const float CenterX = 960f;
        private const float UpperY = 360f;
        private const float LowerY = 475f;
        private const float MaxLowerWidth = 1100f;   // максимальная ширина области для нижнего текста

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

            _upperText = new TextObject(ActivePhrase.Text, CenterX, UpperY, 68f)
            {
                Color = new Color4(0.85f, 0.88f, 0.95f, 0.95f),
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
            const float baseLetterSpacing = 18f;     // увеличил

            var font = FontManager.Get("default") ?? FontManager._defaultFont;
            if (font == null) return;

            float scale = font.GetScaleFromFontSize(baseFontSize);

            // === Расчёт естественной ширины ===
            float totalNaturalWidth = 0f;
            List<float> charAdvances = new(displayText.Length);
            char prev = '\0';

            for (int i = 0; i < displayText.Length; i++)
            {
                char c = displayText[i];
                float advance = 0f;

                if (font.TryGetRenderedGlyph(c, out var glyph) && glyph != null)
                {
                    float kerning = prev != '\0' ? font.GetKerning(prev, c) : 0f;
                    advance = (kerning + glyph.Info.XAdvance) * scale;
                }
                else
                {
                    advance = baseFontSize * 0.65f;
                }

                charAdvances.Add(advance);
                totalNaturalWidth += advance;
                prev = c;
            }

            // === Динамическое масштабирование, если текст слишком длинный ===
            float totalSpacingWidth = baseLetterSpacing * (displayText.Length - 1);
            float totalWidth = totalNaturalWidth + totalSpacingWidth;

            float finalScale = 1f;
            if (totalWidth > MaxLowerWidth)
            {
                finalScale = MaxLowerWidth / totalWidth;
            }

            float finalFontSize = baseFontSize * finalScale;
            float finalLetterSpacing = baseLetterSpacing * finalScale;

            // Пересчитываем позиции с учётом масштаба
            float startX = CenterX - (totalNaturalWidth * finalScale + finalLetterSpacing * (displayText.Length - 1)) / 2f;
            float currentX = startX;

            for (int i = 0; i < displayText.Length; i++)
            {
                bool hasSlider = ActivePhrase.Sliders.Any(s => s.charIndex == i);

                var charObj = new TextObject(displayText[i].ToString(), currentX, LowerY, finalFontSize)
                {
                    AllowHover = !hasSlider,
                    FixedColor = hasSlider,
                    Color = hasSlider ? new Color4(1f, 0.35f, 0.35f, 1f) : Color4.White,
                    Align = TextAlign.Left,
                    ScaleMultiply = 1f,
                    Layer = 5,
                    FontKey = "UI"
                };

                if (!hasSlider)
                {
                    int idx = i;
                    charObj.OnClick = _ => OnSliderRequested?.Invoke(ActivePhrase!, idx);
                }

                _scene.Add(charObj);
                _lowerChars.Add(charObj);

                currentX += charAdvances[i] * finalScale + finalLetterSpacing;
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