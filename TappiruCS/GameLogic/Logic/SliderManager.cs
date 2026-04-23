using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;

namespace TappiruCS.GameLogic.Logic
{
    /// <summary>
    /// Управляет слайдерами текущей фазы.
    ///
    /// Жизненный цикл слайдера:
    ///   1. TryStartSlider()  — игрок нажимает нужную клавишу.
    ///      Проверяем |currentTime - slider.startTime|:
    ///        ≤ PerfectStartWindow → startQuality = 1.0 (Perfect)
    ///        ≤ GoodStartWindow    → startQuality = 0.5 (Good)
    ///        иначе                → false (мисс, фаза перешла к следующему символу)
    ///
    ///   2. Update() вызывается каждый кадр пока IsHoldingSlider = true:
    ///      a) Если currentTime >= slider.endTime — авто-релиз:
    ///         очки = startQuality (только старт влияет на качество).
    ///      b) Если клавиша отпущена раньше — ручной релиз:
    ///         Проверяем |currentTime - slider.endTime|:
    ///           ≤ PerfectEndWindow → releaseQuality = 1.0
    ///           ≤ GoodEndWindow    → releaseQuality = 0.5
    ///           иначе              → мисс
    ///         finalMultiplier = min(startQuality, releaseQuality).
    ///
    ///   3. OnSliderCompleted вызывается PhaseManager-ом для перехода к след. символу.
    /// </summary>
    public class SliderManager
    {
        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Слайдер успешно завершён. Аргумент — charIndex слайдера.</summary>
        public event Action<int> OnSliderCompleted;

        /// <summary>Слайдер успешно завершён с определённым качеством. 1.0 = Perfect, 0.5 = Good.</summary>
        public event Action<float> OnSliderHit;

        // ── Public state ──────────────────────────────────────────────────────────
        public Dictionary<int, SliderTiming> CurrentSliders { get; private set; } = new();
        public bool IsHoldingSlider { get; private set; }
        public int CurrentSliderCharIndex { get; private set; } = -1;

        public IReadOnlySet<int> SuccessfullyCompletedSliders => _completedSliders;
        public IReadOnlySet<int> SuccessfullyHeldSliders => _heldSliders;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly MapData _mapData;
        private readonly Dictionary<char, Keys> _charToKeyMap;

        private readonly HashSet<int> _completedSliders = new();
        private readonly HashSet<int> _heldSliders = new();

        private Keys _heldKey;
        private float _startQuality; // 1.0 = Perfect, 0.5 = Good

        // ── Constructor ───────────────────────────────────────────────────────────
        public SliderManager(MapData mapData, Dictionary<char, Keys> charToKeyMap)
        {
            _mapData = mapData;
            _charToKeyMap = charToKeyMap;
        }

        // ── Phase lifecycle ───────────────────────────────────────────────────────

        /// <summary>
        /// Загружает слайдеры новой фазы. Вызывается один раз при активации фазы.
        /// </summary>
        public void LoadPhaseSliders(Dictionary<int, SliderTiming> sliders)
        {
            CurrentSliders = sliders ?? new Dictionary<int, SliderTiming>();
            _completedSliders.Clear();
            _heldSliders.Clear();
            ClearHoldState();
        }

        // ── Input handling ────────────────────────────────────────────────────────

        /// <summary>
        /// Попытка начать слайдер на позиции charIndex.
        /// Возвращает true если слайдер успешно начался.
        /// Возвращает false если мимо окна (внешний код должен засчитать мисс).
        /// </summary>
        public bool TryStartSlider(char inputChar, char expected, double currentTime, int charIndex)
        {
            // Уже завершённый слайдер трогать нельзя
            if (_completedSliders.Contains(charIndex))
                return false;

            if (!CurrentSliders.TryGetValue(charIndex, out var slider))
                return false;

            // Уже держим этот конкретный слайдер — всё ок
            if (IsHoldingSlider && CurrentSliderCharIndex == charIndex)
                return true;

            // Проверяем окно старта
            double delta = Math.Abs(currentTime - slider.startTime);

            if (delta <= _mapData.GlobalSliderPerfectStartWindow)
            {
                _startQuality = 1.0f;
                Console.WriteLine($"[Slider] Start Perfect | char={charIndex} delta={delta:F3}s");
            }
            else if (delta <= _mapData.GlobalSliderGoodStartWindow)
            {
                _startQuality = 0.5f;
                Console.WriteLine($"[Slider] Start Good    | char={charIndex} delta={delta:F3}s");
            }
            else
            {
                Console.WriteLine($"[Slider] Start Miss    | char={charIndex} delta={delta:F3}s > goodWin={_mapData.GlobalSliderGoodStartWindow:F3}s");
                return false; // мисс — внешний код обрабатывает
            }

            IsHoldingSlider = true;
            CurrentSliderCharIndex = charIndex;
            _charToKeyMap.TryGetValue(inputChar, out _heldKey);
            return true;
        }

        // ── Frame update ──────────────────────────────────────────────────────────

        public void Update(double currentTime, KeyboardState keyboard)
        {
            if (!IsHoldingSlider) return;

            if (!CurrentSliders.TryGetValue(CurrentSliderCharIndex, out var slider))
            {
                ClearHoldState();
                return;
            }

            // Авто-релиз: удержали до конца — качество определяется только стартом
            if (currentTime >= slider.endTime)
            {
                CompleteSlider(CurrentSliderCharIndex, _startQuality);
                return;
            }

            // Ручной релиз: клавиша отпущена раньше времени
            if (!keyboard.IsKeyDown(_heldKey))
            {
                HandleManualRelease(currentTime, slider);
            }
        }

        // ── Reset ─────────────────────────────────────────────────────────────────

        public void ResetHolding() => ClearHoldState();

        public void Reset()
        {
            CurrentSliders.Clear();
            _completedSliders.Clear();
            _heldSliders.Clear();
            ClearHoldState();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleManualRelease(double currentTime, SliderTiming slider)
        {
            int charIndex = CurrentSliderCharIndex;
            double delta = Math.Abs(currentTime - slider.endTime);

            float releaseQuality;

            if (delta <= _mapData.GlobalSliderPerfectEndWindow)
                releaseQuality = 1.0f;
            else if (delta <= _mapData.GlobalSliderGoodEndWindow)
                releaseQuality = 0.5f;
            else
                releaseQuality = 0f; // мимо — мисс

            float finalMultiplier = Math.Min(_startQuality, releaseQuality);

            Console.WriteLine(
                $"[Slider] Manual release | char={charIndex} " +
                $"startQ={_startQuality:F1} releaseQ={releaseQuality:F1} finalQ={finalMultiplier:F1}");

            if (finalMultiplier > 0f)
            {
                CompleteSlider(charIndex, finalMultiplier);
            }
            else
            {
                // Отпустили слишком рано — мисс
                Console.WriteLine($"[Slider] Miss (released too early) | char={charIndex}");
                ClearHoldState();
                // NOTE: PhaseManager должен получить это событие.
                // Пока мисс слайдера засчитывается как мисс символа в HandleInput.
            }
        }

        /// <summary>
        /// Финализирует успешный слайдер: обновляет сеты, кидает события, сбрасывает удержание.
        /// </summary>
        private void CompleteSlider(int charIndex, float finalMultiplier)
        {
            _completedSliders.Add(charIndex);
            _heldSliders.Add(charIndex);

            OnSliderHit?.Invoke(finalMultiplier);
            OnSliderCompleted?.Invoke(charIndex);

            ClearHoldState();
        }

        private void ClearHoldState()
        {
            IsHoldingSlider = false;
            CurrentSliderCharIndex = -1;
            _startQuality = 0f;
        }
    }
}