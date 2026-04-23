using System;
using TappiruCS.GameLogic.Logic;

namespace TappiruCS.GameLogic.Logic
{
    public class PhaseManager
    {
        // ── Public state ──────────────────────────────────────────────────────────
        public bool IsActivePhase { get; private set; }
        public bool PhaseComplete { get; private set; }
        public int CurrentCharIndex { get; private set; }
        public int CompletedPhases { get; private set; }
        public int FailedPhases { get; private set; }

        public string CurrentPhaseText { get; private set; } = string.Empty;
        public char[] CurrentPhaseChars { get; private set; } = Array.Empty<char>();
        public double CurrentPhaseStartTime { get; private set; }
        public double CurrentPhaseEndTime { get; private set; }

        public bool IsMapCompleted => _currentPhaseIndex >= _mapData.Events.Count;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly MapData _mapData;
        private readonly ScoringSystem _scoring;
        private readonly HealthSystem _health;
        private readonly SliderManager _sliderManager;

        private int _currentPhaseIndex;
        private bool _phaseEndHandled;
        private double _nextPhaseStartTime;

        // ── Constructor ───────────────────────────────────────────────────────────
        public PhaseManager(MapData mapData, ScoringSystem scoring, HealthSystem health, SliderManager sliderManager)
        {
            _mapData = mapData;
            _scoring = scoring;
            _health = health;
            _sliderManager = sliderManager;

            // Слайдер успешно завершён — решаем что делать с фазой
            _sliderManager.OnSliderCompleted += OnSliderCompleted;

            // Слайдер завершён — засчитываем очки и здоровье
            _sliderManager.OnSliderHit += OnSliderHit;
        }

        // ── Update ────────────────────────────────────────────────────────────────
        public void Update(double currentTime)
        {
            TryActivateNextPhase(currentTime);

            if (!IsActivePhase) return;

            AdvancePastSpaces();
            TryHandlePhaseTimeout(currentTime);
        }

        // ── Input ─────────────────────────────────────────────────────────────────
        public void HandleInput(char inputChar, double currentTime)
        {
            if (!IsActivePhase || PhaseComplete) return;

            AdvancePastSpaces();

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
            {
                FinishPhaseSuccess();
                return;
            }

            char expected = CurrentPhaseChars[CurrentCharIndex];

            // Уже держим слайдер на этой позиции — ждём релиза
            if (_sliderManager.IsHoldingSlider && _sliderManager.CurrentSliderCharIndex == CurrentCharIndex)
                return;

            bool isSlider = _sliderManager.CurrentSliders.ContainsKey(CurrentCharIndex);

            if (isSlider)
            {
                HandleSliderInput(inputChar, expected, currentTime);
            }
            else
            {
                HandleRegularTap(inputChar, expected);
            }
        }

        public bool IsInputAllowed(double currentTime)
        {
            if (!IsActivePhase) return false;

            const double graceBefore = 0.08;
            return currentTime >= CurrentPhaseStartTime - graceBefore
                && currentTime <= CurrentPhaseEndTime;
        }

        // ── Reset ─────────────────────────────────────────────────────────────────
        public void Reset()
        {
            _currentPhaseIndex = 0;
            IsActivePhase = false;
            PhaseComplete = false;
            _phaseEndHandled = false;
            CurrentCharIndex = 0;
            CurrentPhaseText = string.Empty;
            CurrentPhaseChars = Array.Empty<char>();
            CompletedPhases = 0;
            FailedPhases = 0;
        }

        // ── Private: phase activation ─────────────────────────────────────────────
        private void TryActivateNextPhase(double currentTime)
        {
            if (IsActivePhase || _currentPhaseIndex >= _mapData.Events.Count) return;

            var ev = _mapData.Events[_currentPhaseIndex];

            _nextPhaseStartTime = (_currentPhaseIndex + 1 < _mapData.Events.Count)
                ? _mapData.Events[_currentPhaseIndex + 1].startTime
                : double.PositiveInfinity;

            if (currentTime < ev.startTime || currentTime >= _nextPhaseStartTime) return;

            // Активируем фазу
            CurrentPhaseText = ev.text;
            CurrentPhaseChars = ev.text.ToCharArray();
            CurrentPhaseStartTime = ev.startTime;
            CurrentPhaseEndTime = ev.endTime;
            CurrentCharIndex = 0;
            IsActivePhase = true;
            PhaseComplete = false;
            _phaseEndHandled = false;

            // Сбрасываем слайдер предыдущей фазы если вдруг остался
            if (_sliderManager.IsHoldingSlider)
                _sliderManager.ResetHolding();

            // БАГ ИСПРАВЛЕН: LoadPhaseSliders вызывается ОДИН раз
            var sliderMap = ev.sliders?.ToDictionary(s => s.charIndex) ?? new();
            _sliderManager.LoadPhaseSliders(sliderMap);
        }

        // ── Private: input helpers ────────────────────────────────────────────────
        private void HandleSliderInput(char inputChar, char expected, double currentTime)
        {
            bool started = _sliderManager.TryStartSlider(inputChar, expected, currentTime, CurrentCharIndex);

            if (!started)
            {
                // Мимо окна — засчитываем мисс, пропускаем этот символ
                _scoring.RegisterMiss();
                _health.Miss();
                CurrentCharIndex++;

                if (CurrentCharIndex >= CurrentPhaseChars.Length)
                    FinishPhaseSuccess();
            }
            // Иначе слайдер начался — ждём OnSliderCompleted
        }

        private void HandleRegularTap(char inputChar, char expected)
        {
            if (inputChar != expected)
            {
                _scoring.BreakCombo();
                _health.Miss();
                return;
            }

            CurrentCharIndex++;
            _scoring.RegisterHit();
            _health.GainTap();

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
                FinishPhaseSuccess();
        }

        // ── Private: slider callbacks ─────────────────────────────────────────────
        private void OnSliderHit(float gradeMultiplier)
        {
            _scoring.RegisterSliderHit(gradeMultiplier);

            if (gradeMultiplier >= 1f)
                _health.GainSliderPerfect();
            else
                _health.GainSliderGood();
        }

        private void OnSliderCompleted(int sliderCharIndex)
        {
            // Переходим к следующему символу после слайдера
            CurrentCharIndex = sliderCharIndex + 1;

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
                FinishPhaseSuccess();
        }

        // ── Private: phase timeout ────────────────────────────────────────────────
        private void TryHandlePhaseTimeout(double currentTime)
        {
            if (currentTime < _nextPhaseStartTime || _phaseEndHandled) return;

            _phaseEndHandled = true;
            IsActivePhase = false;

            _sliderManager.ResetHolding();

            int remaining = CurrentPhaseChars.Length - CurrentCharIndex;

            // Все символы пройдены (включая тех что могли быть слайдерами)
            if (remaining <= 0)
            {
                // Фаза уже была завершена через FinishPhaseSuccess, просто двигаем индекс
                _currentPhaseIndex++;
                CurrentCharIndex = 0;
                return;
            }

            // Не все символы пройдены — фаза провалена
            FailedPhases++;
            _scoring.BreakCombo();
            _scoring.Misses += remaining;
            _health.PhaseFail(remaining);
            PhaseComplete = false;

            _currentPhaseIndex++;
            CurrentCharIndex = 0;
        }

        // ── Private: phase complete ───────────────────────────────────────────────
        private void FinishPhaseSuccess()
        {
            PhaseComplete = true;
            IsActivePhase = false;

            _sliderManager.ResetHolding();

            CompletedPhases++;
            _scoring.RegisterPhaseComplete();
            _health.GainPhaseComplete();

            _currentPhaseIndex++;
            CurrentCharIndex = 0;
        }

        // ── Private: utilities ────────────────────────────────────────────────────
        private void AdvancePastSpaces()
        {
            while (CurrentCharIndex < CurrentPhaseChars.Length
                   && CurrentPhaseChars[CurrentCharIndex] == ' ')
            {
                CurrentCharIndex++;
            }
        }
    }
}