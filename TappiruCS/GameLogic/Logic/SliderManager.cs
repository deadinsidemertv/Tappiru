using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;

namespace TappiruCS.GameLogic.Logic
{
    public class SliderManager
    {
        public Dictionary<int, SliderTiming> CurrentSliders { get; private set; } = new();
        public bool IsHoldingSlider { get; private set; }
        public int CurrentSliderCharIndex { get; private set; } = -1;

        private Keys _heldKey;
        private readonly MapData _mapData;
        private readonly Dictionary<char, Keys> _charToKeyMap;

        private readonly HashSet<int> _successfullyCompletedSliders = new();
        private readonly HashSet<int> _successfullyHeldSliders = new();

        public IReadOnlySet<int> SuccessfullyCompletedSliders => _successfullyCompletedSliders;
        public IReadOnlySet<int> SuccessfullyHeldSliders => _successfullyHeldSliders;

        // Новое: событие, которое уведомляет PhaseManager, что слайдер успешно завершён
        public event Action<int> OnSliderSuccessfullyReleased;

        public SliderManager(MapData mapData, Dictionary<char, Keys> charToKeyMap)
        {
            _mapData = mapData;
            _charToKeyMap = charToKeyMap;
        }

        public void LoadPhaseSliders(Dictionary<int, SliderTiming> sliders)
        {
            CurrentSliders = sliders ?? new Dictionary<int, SliderTiming>();
            ResetHolding();
            _successfullyCompletedSliders.Clear();
            _successfullyHeldSliders.Clear();
        }

        public bool TryStartSlider(char inputChar, char expected, double currentTime, int charIndex)
        {
            // Защита: если слайдер уже успешно завершён — не начинаем заново
            if (_successfullyCompletedSliders.Contains(charIndex))
                return false;

            if (!CurrentSliders.TryGetValue(charIndex, out var slider))
                return false;

            if (IsHoldingSlider && CurrentSliderCharIndex == charIndex)
                return true;

            double delta = Math.Abs(currentTime - slider.startTime);
            if (delta > _mapData.GlobalSliderGoodStartWindow)
                return false;

            IsHoldingSlider = true;
            CurrentSliderCharIndex = charIndex;

            _charToKeyMap.TryGetValue(inputChar, out _heldKey);
            return true;
        }

        public void Update(double currentTime, KeyboardState keyboard)
        {
            if (!IsHoldingSlider) return;

            var slider = CurrentSliders[CurrentSliderCharIndex];

            // Автоматическое засчитывание холда по времени
            if (currentTime >= slider.endTime && !_successfullyHeldSliders.Contains(CurrentSliderCharIndex))
            {
                _successfullyHeldSliders.Add(CurrentSliderCharIndex);
            }

            // Релиз клавиши
            if (!keyboard.IsKeyDown(_heldKey))
            {
                HandleSliderRelease(currentTime);
            }
        }

        private void HandleSliderRelease(double currentTime)
        {
            if (!IsHoldingSlider) return;

            var slider = CurrentSliders[CurrentSliderCharIndex];
            double delta = Math.Abs(currentTime - slider.endTime);

            bool isSuccess = delta <= _mapData.GlobalSliderPerfectEndWindow ||
                             delta <= _mapData.GlobalSliderGoodEndWindow;

            if (isSuccess)
            {
                _successfullyCompletedSliders.Add(CurrentSliderCharIndex);
                _successfullyHeldSliders.Add(CurrentSliderCharIndex);

                // Уведомляем PhaseManager, что нужно двигаться дальше
                OnSliderSuccessfullyReleased?.Invoke(CurrentSliderCharIndex);
            }

            ResetHolding();
        }

        public void ResetHolding()
        {
            IsHoldingSlider = false;
            CurrentSliderCharIndex = -1;
        }

        public void Reset()
        {
            CurrentSliders.Clear();
            ResetHolding();
            _successfullyCompletedSliders.Clear();
            _successfullyHeldSliders.Clear();
        }
    }
}