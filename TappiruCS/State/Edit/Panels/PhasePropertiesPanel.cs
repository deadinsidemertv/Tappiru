using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.State.Edit.TimelineSystem;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit.UI.Panels
{
    internal class PhrasePropertiesPanel
    {
        private readonly Scene _scene;
        private readonly PhraseTextDisplay _phraseDisplay;
        private readonly Timeline _timeline;                 // ← Добавили для обновления визуалов

        private Container PhraseProperties = null!;

        private InputField? _inputDisplay;
        private InputField? _inputTrans;
        private InputField? _inputStartTime;
        private InputField? _inputEndTime;

        private Phrase? _currentPhrase;

        public PhrasePropertiesPanel(Scene scene, PhraseTextDisplay phraseDisplay, Timeline timeline)
        {
            _scene = scene;
            _phraseDisplay = phraseDisplay;
            _timeline = timeline;
        }

        public void Build()
        {
            RebuildPanel(null);
        }

        private void RebuildPanel(ITimelineSelectable? selected)
        {
            _scene.Remove(PhraseProperties);

            PhraseProperties = new Container(1743, 420);

            var label = new TextObject("выбранный объект", -160, -270, 36f);
            label.Color = "#919bb8";
            label.Align = TextAlign.Left;


            var spriteBackground = new SpriteObject(TextureManager.GetTexture("blue_panel"), 0, -110, 345, 400);

            PhraseProperties.AddChild(spriteBackground);
            PhraseProperties.AddChild(label);

            if (selected is Phrase phrase)
            {
                _currentPhrase = phrase;

                // === Текст ===
                var DisplayText = new TextObject("Текст", -160, -220, 28f);
                DisplayText.Color = "#919bb8";
                DisplayText.Align = TextAlign.Left;

                _inputDisplay = new InputField(-7, -190, 310, 35);
                _inputDisplay.LeftPadding = 12f;
                _inputDisplay.Text = phrase.Text ?? "";
                _inputDisplay.OnTextChanged += OnDisplayTextChanged;

                // === Транскрипция ===
                var TransText = new TextObject("Транскрипция", -160, -150, 28f);
                TransText.Color = "#919bb8";
                TransText.Align = TextAlign.Left;

                _inputTrans = new InputField(-7, -120, 310, 35);
                _inputTrans.LeftPadding = 12f;
                _inputTrans.Text = phrase.Transcription ?? "";
                _inputTrans.OnTextChanged += OnTranscriptionChanged;

                // === Время: Начало ===
                var StartLabel = new TextObject("Начало (сек)", -160, -80, 28f);
                StartLabel.Color = "#919bb8";
                StartLabel.Align = TextAlign.Left;

                _inputStartTime = new InputField(-7, -50, 150, 35);
                _inputStartTime.LeftPadding = 12f;
                _inputStartTime.Text = phrase.StartTime.ToString("F2");
                _inputStartTime.OnTextChanged += OnStartTimeChanged;

                // === Время: Конец ===
                var EndLabel = new TextObject("Конец (сек)", -160, -10, 28f);  // чуть ниже
                EndLabel.Color = "#919bb8";
                EndLabel.Align = TextAlign.Left;

                _inputEndTime = new InputField(-7, 20, 150, 35);
                _inputEndTime.LeftPadding = 12f;
                _inputEndTime.Text = phrase.EndTime.ToString("F2");
                _inputEndTime.OnTextChanged += OnEndTimeChanged;

                var delete = new Button(0, 150, 200, 70, "blue_panel", "удалить");
                delete.Label.Color = Color4.Red;
                delete.Label.FontSize = 36f;
                delete.Label.FontKey = "Game";
                delete.Label.Align = TextAlign.Center;
                delete.Layer = 5;
                delete.OnClick += DeleteObject;
                _scene.Add(delete);

                // Добавляем всё на панель
                PhraseProperties.AddChild(DisplayText);
                PhraseProperties.AddChild(_inputDisplay);

                PhraseProperties.AddChild(TransText);
                PhraseProperties.AddChild(_inputTrans);

                PhraseProperties.AddChild(StartLabel);
                PhraseProperties.AddChild(_inputStartTime);

                PhraseProperties.AddChild(EndLabel);
                PhraseProperties.AddChild(_inputEndTime);

                PhraseProperties.AddChild(delete);
            }
            else if (selected is TappiruCS.State.Edit.Core.SliderTiming)
            {
                var sliderLabel = new TextObject("Слайдер", 0, 0, 28f);
                var spriteSliderBg = new SpriteObject(TextureManager.GetTexture("window-module_3"), 0, 0, 300, 600);
                PhraseProperties.AddChild(spriteSliderBg);
                PhraseProperties.AddChild(sliderLabel);
            }
            else
            {
                _currentPhrase = null;
                _inputDisplay = _inputTrans = _inputStartTime = _inputEndTime = null;
            }

            _scene.Add(PhraseProperties);
        }

        public void Sync(ITimelineSelectable? selected)
        {
            if (selected is Phrase phrase && _currentPhrase == phrase)
            {
                if (_inputDisplay != null) _inputDisplay.Text = phrase.Text ?? "";
                if (_inputTrans != null) _inputTrans.Text = phrase.Transcription ?? "";
                if (_inputStartTime != null) _inputStartTime.Text = phrase.StartTime.ToString("F2");
                if (_inputEndTime != null) _inputEndTime.Text = phrase.EndTime.ToString("F2");
                return;
            }

            RebuildPanel(selected);
        }

        // ====================== ОБРАБОТЧИКИ ======================

        private void OnDisplayTextChanged(string newText)
        {
            if (_currentPhrase != null)
            {
                _currentPhrase.Text = newText;
                _phraseDisplay.Sync(_currentPhrase);
            }
        }

        private void OnTranscriptionChanged(string newText)
        {
            if (_currentPhrase != null)
            {
                _currentPhrase.Transcription = newText;
                _phraseDisplay.Sync(_currentPhrase);
            }
        }

        private void OnStartTimeChanged(string newText)
        {
            if (_currentPhrase == null || !float.TryParse(newText, out float newStart)) return;

            float oldStart = _currentPhrase.StartTime;
            _currentPhrase.StartTime = Math.Clamp(newStart, 0f, _currentPhrase.EndTime - 0.1f);

            if (_currentPhrase.StartTime != oldStart)
            {
                _phraseDisplay.Sync(_currentPhrase);
                _timeline.RefreshAllVisuals();
            }
        }

        private void OnEndTimeChanged(string newText)
        {
            if (_currentPhrase == null || !float.TryParse(newText, out float newEnd)) return;

            float oldEnd = _currentPhrase.EndTime;
            _currentPhrase.EndTime = Math.Clamp(newEnd, _currentPhrase.StartTime + 0.1f, _timeline.TotalDuration);

            if (_currentPhrase.EndTime != oldEnd)
            {
                _phraseDisplay.Sync(_currentPhrase);
                _timeline.RefreshAllVisuals();
            }
        }

        private void DeleteObject()
        {
            if (_currentPhrase != null)
            {
                _timeline._phrases.Remove(_currentPhrase);
            }
        }
    }
}