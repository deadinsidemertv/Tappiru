using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.State.Edit.TimelineSystem;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.UI.Sprite;

namespace TappiruCS.State.Edit.UI.Panels
{
    internal class PhrasePropertiesPanel
    {
        private readonly Scene _scene;
        private readonly PhraseTextDisplay _phraseDisplay;
        private readonly Timeline _timeline;

        private Container PhraseProperties = null!;

        private InputField? _inputDisplay;
        private InputField? _inputTrans;
        private InputField? _inputStartTime;
        private InputField? _inputEndTime;

        private InputField? _inputSliderStartTime;
        private InputField? _inputSliderEndTime;

        private ITimelineSelectable? _currentSelectable;
        private Phrase? _currentPhrase;

        private readonly List<Phrase> _globalPhrases;

        private readonly EditState _editState;

        // Костыль: храним ссылку на текущую кнопку удаления, чтобы удалять её из сцены при перестройке
        private Button? _currentDeleteButton;

        public PhrasePropertiesPanel(Scene scene, PhraseTextDisplay phraseDisplay, Timeline timeline, List<Phrase> globalPhrases,EditState editState)
        {
            _scene = scene;
            _phraseDisplay = phraseDisplay;
            _timeline = timeline;
            _globalPhrases = globalPhrases;
            _editState = editState;
        }

        public void Build()
        {
            RebuildPanel(null);
        }

        private void RebuildPanel(ITimelineSelectable? selected)
        {
            if (PhraseProperties != null)
            {
                _scene.Remove(PhraseProperties);
                PhraseProperties = null!;
            }

            // Создаём новую панель
            PhraseProperties = new Container(1743, 420);

            // Фон
            var spriteBackground = new NineSliceSprite(TextureManager.GetTexture("blue_panel"), 0, -110, 345, 400);
            PhraseProperties.AddChild(spriteBackground);

            // Заголовок всегда "Свойства"
            var title = new TextObject("Свойства", -160, -270, 36f);
            title.Color = "#919bb8";
            title.Align = TextAlign.Left;
            PhraseProperties.AddChild(title);

            // Ничего не выбрано – только фон и заголовок
            if (selected == null)
            {
                _scene.Add(PhraseProperties);
                ClearInputs();
                return;
            }

            if (selected is Phrase phrase)
            {
                _currentSelectable = phrase;
                _currentPhrase = phrase;

                AddPhraseControls(phrase);
            }
            else if (selected is SliderTiming slider)
            {
                _currentSelectable = slider;
                _currentPhrase = null;

                AddSliderControls(slider);
            }

            _scene.Add(PhraseProperties);
        }
        private void AddPhraseControls(Phrase phrase)
        {
            // Текст
            _inputDisplay = new InputField(-7, -190, 310, 35) { LeftPadding = 12f, Text = phrase.Text ?? "" };
            _inputDisplay.OnTextChanged += OnDisplayTextChanged;
            PhraseProperties.AddChild(_inputDisplay);

            _inputTrans = new InputField(-7, -120, 310, 35) { LeftPadding = 12f, Text = phrase.Transcription ?? "" };
            _inputTrans.OnTextChanged += OnTranscriptionChanged;
            PhraseProperties.AddChild(_inputTrans);

            _currentSelectable = phrase;
            _currentPhrase = phrase;

            var displayLabel = new TextObject("Текст", -160, -220, 28f);
            displayLabel.Color = "#919bb8";
            displayLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(displayLabel);

            _inputDisplay = new InputField(-7, -190, 310, 35);
            _inputDisplay.LeftPadding = 12f;
            _inputDisplay.Text = phrase.Text ?? "";
            _inputDisplay.OnTextChanged += OnDisplayTextChanged;
            PhraseProperties.AddChild(_inputDisplay);

            var transLabel = new TextObject("Транскрипция", -160, -150, 28f);
            transLabel.Color = "#919bb8";
            transLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(transLabel);

            _inputTrans = new InputField(-7, -120, 310, 35);
            _inputTrans.LeftPadding = 12f;
            _inputTrans.Text = phrase.Transcription ?? "";
            _inputTrans.OnTextChanged += OnTranscriptionChanged;
            PhraseProperties.AddChild(_inputTrans);

            var startLabel = new TextObject("Начало (сек)", -160, -80, 28f);
            startLabel.Color = "#919bb8";
            startLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(startLabel);

            _inputStartTime = new InputField(-7, -50, 150, 35);
            _inputStartTime.LeftPadding = 12f;
            _inputStartTime.Text = phrase.StartTime.ToString("F2");
            _inputStartTime.OnTextChanged += OnStartTimeChanged;
            PhraseProperties.AddChild(_inputStartTime);

            var endLabel = new TextObject("Конец (сек)", -160, -10, 28f);
            endLabel.Color = "#919bb8";
            endLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(endLabel);

            _inputEndTime = new InputField(-7, 20, 150, 35);
            _inputEndTime.LeftPadding = 12f;
            _inputEndTime.Text = phrase.EndTime.ToString("F2");
            _inputEndTime.OnTextChanged += OnEndTimeChanged;
            PhraseProperties.AddChild(_inputEndTime);

            // === Кнопка удаления ===
            var delete = new Button(0, 150, 200, 70, "blue_panel", "удалить фразу");
            delete.Label.Color = Color4.Red;
            delete.Label.FontSize = 28f;
            delete.Label.FontKey = "Game";
            delete.Label.Align = TextAlign.Center;
            delete.Layer = 5;
            delete.OnClick += DeleteObject;

            PhraseProperties.AddChild(delete);     // ← Правильно: как ребёнок панели
            _currentDeleteButton = delete;
        }

        private void AddSliderControls(SliderTiming slider)
        {
            _currentSelectable = slider;
            _currentPhrase = null;

            var sliderLabel = new TextObject("Слайдер", -160, -220, 28f);
            sliderLabel.Color = "#919bb8";
            sliderLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(sliderLabel);

            var startLabel = new TextObject("Начало (сек)", -160, -160, 28f);
            startLabel.Color = "#919bb8";
            startLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(startLabel);

            _inputSliderStartTime = new InputField(-7, -130, 150, 35);
            _inputSliderStartTime.LeftPadding = 12f;
            _inputSliderStartTime.Text = slider.startTime.ToString("F2");
            _inputSliderStartTime.OnTextChanged += OnSliderStartTimeChanged;
            PhraseProperties.AddChild(_inputSliderStartTime);

            var endLabel = new TextObject("Конец (сек)", -160, -90, 28f);
            endLabel.Color = "#919bb8";
            endLabel.Align = TextAlign.Left;
            PhraseProperties.AddChild(endLabel);

            _inputSliderEndTime = new InputField(-7, -60, 150, 35);
            _inputSliderEndTime.LeftPadding = 12f;
            _inputSliderEndTime.Text = slider.endTime.ToString("F2");
            _inputSliderEndTime.OnTextChanged += OnSliderEndTimeChanged;
            PhraseProperties.AddChild(_inputSliderEndTime);

            // Кнопка удаления
            var delete = new Button(0, 20, 200, 70, "blue_panel", "удалить слайдер");
            delete.Label.Color = Color4.Red;
            delete.Label.FontSize = 28f;
            delete.Label.FontKey = "Game";
            delete.Label.Align = TextAlign.Center;
            delete.Layer = 5;
            delete.OnClick += DeleteObject;

            PhraseProperties.AddChild(delete);     // ← Правильно
            _currentDeleteButton = delete;
        }

        private void ClearInputs()
        {
            _currentSelectable = null;
            _currentPhrase = null;
            _inputDisplay = _inputTrans = _inputStartTime = _inputEndTime = null;
            _inputSliderStartTime = _inputSliderEndTime = null;
        }

        public void Sync(ITimelineSelectable? selected)
        {
            if (selected == null || selected != _currentSelectable)
            {
                RebuildPanel(selected);
                return;
            }

            if (selected is Phrase phrase && _currentPhrase == phrase)
            {
                if (_inputDisplay != null) _inputDisplay.Text = phrase.Text ?? "";
                if (_inputTrans != null) _inputTrans.Text = phrase.Transcription ?? "";
                if (_inputStartTime != null) _inputStartTime.Text = phrase.StartTime.ToString("F2");
                if (_inputEndTime != null) _inputEndTime.Text = phrase.EndTime.ToString("F2");
            }
            else if (selected is SliderTiming slider && _currentSelectable == slider)
            {
                if (_inputSliderStartTime != null) _inputSliderStartTime.Text = slider.startTime.ToString("F2");
                if (_inputSliderEndTime != null) _inputSliderEndTime.Text = slider.endTime.ToString("F2");
            }
        }

        // ===== обработчики для фразы =====
        private void OnDisplayTextChanged(string newText)
        {
            if (_currentPhrase == null) return;

            _currentPhrase.Text = newText;

            // Обновляем Mapping под новую длину текста
            _currentPhrase.ResizeMappingTo(newText.Length);

            // Обновляем визуальное отображение текста
            _phraseDisplay.Sync(_currentPhrase);

            // Если сейчас Mapping Mode — перестраиваем панель маппинга
            if (_editState.currentEditMode == EditMode.Mapping)
            {
                _editState?.RefreshMappingPanel(_currentPhrase);
            }

            _timeline?.RefreshAllVisuals();
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
            if (Math.Abs(_currentPhrase.StartTime - oldStart) > 0.001f)
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
            if (Math.Abs(_currentPhrase.EndTime - oldEnd) > 0.001f)
            {
                _phraseDisplay.Sync(_currentPhrase);
                _timeline.RefreshAllVisuals();
            }
        }

        // ===== обработчики для слайдера =====
        private (float parentStart, float parentEnd) GetParentPhraseTimes(SliderTiming slider)
        {
            foreach (var p in _timeline._phrases)
                if (p.Sliders != null && p.Sliders.Contains(slider))
                    return (p.StartTime, p.EndTime);
            return (0f, _timeline.TotalDuration);
        }

        private void OnSliderStartTimeChanged(string newText)
        {
            if (_currentSelectable is not SliderTiming slider || !float.TryParse(newText, out float newStart)) return;
            var (parentStart, parentEnd) = GetParentPhraseTimes(slider);
            float oldStart = slider.startTime;
            slider.startTime = Math.Clamp(newStart, parentStart, slider.endTime - 0.1f);
            if (Math.Abs(slider.startTime - oldStart) > 0.001f)
                _timeline.RefreshAllVisuals();
        }

        private void OnSliderEndTimeChanged(string newText)
        {
            if (_currentSelectable is not SliderTiming slider || !float.TryParse(newText, out float newEnd)) return;
            var (parentStart, parentEnd) = GetParentPhraseTimes(slider);
            float oldEnd = slider.endTime;
            slider.endTime = Math.Clamp(newEnd, slider.startTime + 0.1f, parentEnd);
            if (Math.Abs(slider.endTime - oldEnd) > 0.001f)
                _timeline.RefreshAllVisuals();
        }

        // ===== удаление =====
        private void DeleteObject()
        {
            if (_currentSelectable == null) return;

            if (_currentSelectable is Phrase phrase)
            {
                _timeline._phrases.Remove(phrase);
                _globalPhrases.Remove(phrase);          // ← добавить
            }
            else if (_currentSelectable is SliderTiming slider)
            {
                foreach (var p in _timeline._phrases)
                {
                    if (p.Sliders != null && p.Sliders.Contains(slider))
                    {
                        p.Sliders.Remove(slider);
                        break;
                    }
                }
                // Для слайдера тоже нужно удалить из глобального списка? 
                // Слайдер — часть фразы, поэтому не нужно отдельно удалять из _globalPhrases.
            }

            _timeline.SelectedObject = null;
            _timeline._draggedPhrase = null;
            _timeline._draggedIndex = -1;
            _timeline.RefreshAllVisuals();

            _phraseDisplay.Sync(null);   // ← очистить отображение текста

            _currentSelectable = null;
            _currentPhrase = null;
            RebuildPanel(null);
        }
    }
}