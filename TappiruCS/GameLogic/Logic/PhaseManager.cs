using System;
using TappiruCS.GameLogic.Logic;

namespace TappiruCS.GameLogic.Logic
{
    public class PhaseManager
    {
        private readonly MapData _mapData;
        private readonly ScoringSystem _scoring;
        private readonly HealthSystem _health;
        private readonly SliderManager _sliderManager;

        public bool IsActivePhase { get; private set; }
        public bool PhaseComplete { get; private set; }
        public int CurrentCharIndex { get; private set; }

        public int CompletedPhases { get; private set; }
        public int FailedPhases { get; private set; }

        public string CurrentPhaseText { get; private set; }
        public char[] CurrentPhaseChars { get; private set; } = Array.Empty<char>();
        public double CurrentPhaseStartTime { get; private set; }
        public double CurrentPhaseEndTime { get; private set; }

        private int _currentPhaseIndex;
        private bool _phaseEndHandled;
        private double _nextPhaseStartTime;

        public PhaseManager(MapData mapData, ScoringSystem scoring, HealthSystem health, SliderManager sliderManager)
        {
            _mapData = mapData;
            _scoring = scoring;
            _health = health;
            _sliderManager = sliderManager;

            _sliderManager.OnSliderSuccessfullyReleased += OnSliderReleased;
        }
        private void OnSliderReleased(int sliderIndex)
        {
            // Если это был последний символ фазы — завершаем фазу
            if (sliderIndex == CurrentPhaseChars.Length - 1)
            {
                CompletePhase();
            }
            else
            {
                // Иначе просто переходим к следующему символу
                CurrentCharIndex = sliderIndex + 1;
            }
        }
        public void Update(double currentTime)
        {


            TryActivateNewPhase(currentTime);
            if (!IsActivePhase) return;

            SkipSpaces();
            TryHandlePhaseTimeout(currentTime);
        }

        public void HandleInput(char inputChar, double currentTime)
        {
            if (!IsActivePhase || PhaseComplete) return;

            SkipSpaces();

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
            {
                CompletePhase();
                return;
            }

            char expected = CurrentPhaseChars[CurrentCharIndex];

            if (_sliderManager.IsHoldingSlider && _sliderManager.CurrentSliderCharIndex == CurrentCharIndex)
                return;

            if (_sliderManager.TryStartSlider(inputChar, expected, currentTime, CurrentCharIndex))
                return;

            HandleRegularTap(inputChar, expected);
        }

        public bool IsInputAllowed(double currentTime)
        {
            if (!IsActivePhase) return false;

            const double graceBefore = 0.08;
            return currentTime >= CurrentPhaseStartTime - graceBefore &&
                   currentTime <= CurrentPhaseEndTime;
        }

        private void TryActivateNewPhase(double currentTime)
        {
            if (IsActivePhase || _currentPhaseIndex >= _mapData.Events.Count) return;

            var ev = _mapData.Events[_currentPhaseIndex];
            _nextPhaseStartTime = _currentPhaseIndex + 1 < _mapData.Events.Count
                ? _mapData.Events[_currentPhaseIndex + 1].startTime
                : double.PositiveInfinity;

            if (currentTime < ev.startTime || currentTime >= _nextPhaseStartTime) return;

            CurrentPhaseText = ev.text;
            CurrentPhaseChars = ev.text.ToCharArray();
            CurrentCharIndex = 0;
            CurrentPhaseStartTime = ev.startTime;
            CurrentPhaseEndTime = ev.endTime;

            IsActivePhase = true;
            PhaseComplete = false;
            _phaseEndHandled = false;

            _sliderManager.LoadPhaseSliders(ev.sliders?.ToDictionary(s => s.charIndex) ?? new());
        }

        private void SkipSpaces()
        {
            while (CurrentCharIndex < CurrentPhaseChars.Length && CurrentPhaseChars[CurrentCharIndex] == ' ')
                CurrentCharIndex++;
        }

        private void TryHandlePhaseTimeout(double currentTime)
        {
            if (currentTime < _nextPhaseStartTime || _phaseEndHandled) return;

            _phaseEndHandled = true;
            IsActivePhase = false;

            bool lastSliderSuccess = _sliderManager.IsHoldingSlider &&
                _sliderManager.CurrentSliderCharIndex == CurrentPhaseChars.Length - 1 &&
                _sliderManager.SuccessfullyHeldSliders.Contains(_sliderManager.CurrentSliderCharIndex);

            _sliderManager.ResetHolding();

            if (lastSliderSuccess)
            {
                CompletedPhases++;
                _scoring.PhaseComplete();
                _health.GainPhaseComplete();
                PhaseComplete = true;
            }
            else if (CurrentCharIndex < CurrentPhaseChars.Length)
            {
                FailedPhases++;
                _scoring.ResetCombo();
                _scoring.Misses += CurrentPhaseChars.Length - CurrentCharIndex;
                _health.PhaseFail(CurrentPhaseChars.Length - CurrentCharIndex);
                PhaseComplete = false;
            }
            else
            {
                PhaseComplete = true;
            }

            _currentPhaseIndex++;
            CurrentCharIndex = 0;
        }

        private void HandleRegularTap(char inputChar, char expected)
        {
            if (inputChar != expected)
            {
                _scoring.ResetCombo();
                _health.Miss();
                return;
            }

            CurrentCharIndex++;
            _scoring.Hit();
            _health.GainTap();

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
                CompletePhase();
        }

        private void CompletePhase()
        {
            PhaseComplete = true;
            IsActivePhase = false;
            _sliderManager.ResetHolding();

            CompletedPhases++;
            _scoring.PhaseComplete();
            _health.GainPhaseComplete();

            _currentPhaseIndex++;
            CurrentCharIndex = 0;
        }

        public bool IsMapCompleted => _currentPhaseIndex >= _mapData.Events.Count && PhaseComplete;

        public void Reset()
        {
            _currentPhaseIndex = 0;
            IsActivePhase = PhaseComplete = _phaseEndHandled = false;
            CurrentCharIndex = 0;
            CurrentPhaseText = string.Empty;
            CurrentPhaseChars = Array.Empty<char>();
            CompletedPhases = FailedPhases = 0;
        }
    }
}