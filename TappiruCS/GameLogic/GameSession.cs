using Gdk;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TappiruCS.GameLogic
{
    public class GameSession
    {
        public static Dictionary<char, Keys> CharToKeyMap { get; private set; }

        public static void InitCharToKeyMap(Dictionary<Keys, char[]> keyToCharsMap)
        {
            CharToKeyMap = new Dictionary<char, Keys>();
            foreach (var kv in keyToCharsMap)
            {
                foreach (char c in kv.Value)
                    if (!CharToKeyMap.ContainsKey(c))
                        CharToKeyMap.Add(c, kv.Key);
            }
        }

        public MapData CurrentMap { get; }
        public double endTime { get; private set; }
        public double CurrentPhaseStartTime { get; private set; }
        public double CurrentPhaseEndTime { get; private set; }

        public string CurrentPhaseText { get; private set; }
        public char[] CurrentPhaseChars { get; private set; }
        public int CurrentCharIndex { get; private set; }
        public bool PhaseComplete { get; private set; }

        public bool IsActivePhase => _isActivePhase;

        public Dictionary<int, SliderTiming> CurrentSliders => _currentSliders;
        public bool IsHoldingSlider => _isHoldingSlider;
        public int CurrentSliderCharIndex => _sliderCharIndex;

        public int TotalScore { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        public int CorrectHits { get; private set; }
        public int Misses { get; private set; }
        public int CompletedPhases { get; private set; }
        public int FailedPhases { get; private set; }

        public float Accuracy { get; private set; }

        public bool IsMapCompleted => _currentPhaseIndex >= CurrentMap.Events.Count && PhaseComplete;

        private int _currentPhaseIndex;
        private bool _isActivePhase;
        private bool _phaseEndHandled;
        private double _nextPhaseStartTime;

        private Dictionary<int, SliderTiming> _currentSliders = new();

        private bool _isHoldingSlider;
        private int _sliderCharIndex = -1;
        private Keys _heldKey;

        private readonly HashSet<int> _successfullyCompletedSliders = new();
        public HashSet<int> SuccessfullyCompletedSliders => _successfullyCompletedSliders;

        private readonly HashSet<int> _successfullyHeldSliders = new();
        public HashSet<int> SuccessfullyHeldSliders => _successfullyHeldSliders;

        private const int PointsPerHit = 100;
        private const int PointsPerPhase = 300;

        public event Action<int> OnComboChanged;        // вызывается при любом изменении комбо
        public event Action OnComboBreak;

        public GameSession(MapData mapData)
        {
            CurrentMap = mapData;
            endTime = mapData.endTime;

            Accuracy = 100f;
            TotalScore = Combo = MaxCombo = 0;
            CorrectHits = Misses = CompletedPhases = FailedPhases = 0;

            _currentPhaseIndex = 0;
            _isActivePhase = false;
            _phaseEndHandled = false;
            _sliderCharIndex = -1;
        }

        /// <summary>
        /// Основной тик логики игры. Вызывается каждый кадр с актуальным временем аудио.
        /// </summary>
        public void Update(double currentTime, KeyboardState keyboard)
        {
            UpdateAccuracy();
            TryActivateNewPhase(currentTime);

            if (!_isActivePhase) return;

            SkipSpacesInPhase();
            TryHandlePhaseTimeout(currentTime);

            if (!_isActivePhase) return;

            HandleOngoingSlider(currentTime);
            TryHandleSliderRelease(keyboard, currentTime);
        }

        /// <summary>
        /// Обработка нажатия клавиши (вызывается из GameSessionState.HandleKeyDown).
        /// </summary>
        public void HandleInput(char inputChar, double currentTime)
        {
            if (!_isActivePhase || PhaseComplete) return;

            SkipSpacesInPhase();

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
            {
                CompletePhase();
                return;
            }

            char expected = CurrentPhaseChars[CurrentCharIndex];

            if (_isHoldingSlider && _sliderCharIndex == CurrentCharIndex)
                return;

            if (TryStartSlider(inputChar, expected, currentTime))
                return;

            HandleRegularTap(inputChar, expected);
        }

        private void UpdateAccuracy()
        {
            Accuracy = (CorrectHits + Misses) > 0
                ? (CorrectHits / (float)(CorrectHits + Misses)) * 100f
                : 100f;
        }

        private void TryActivateNewPhase(double currentTime)
        {
            if (_isActivePhase || _currentPhaseIndex >= CurrentMap.Events.Count)
                return;

            var ev = CurrentMap.Events[_currentPhaseIndex];

            _nextPhaseStartTime = _currentPhaseIndex + 1 < CurrentMap.Events.Count
                ? CurrentMap.Events[_currentPhaseIndex + 1].startTime
                : double.PositiveInfinity;

            if (currentTime >= ev.startTime && currentTime < _nextPhaseStartTime)
            {
                CurrentPhaseText = ev.text;
                CurrentPhaseChars = CurrentPhaseText.ToCharArray();
                CurrentCharIndex = 0;

                // ←←← НОВОЕ: Запоминаем реальное время жизни фразы ←←←
                CurrentPhaseStartTime = ev.startTime;
                CurrentPhaseEndTime = ev.endTime;        // ← используем endTime из мапы!

                _isActivePhase = true;
                PhaseComplete = false;
                _phaseEndHandled = false;

                _currentSliders = ev.sliders?.ToDictionary(s => s.charIndex) ?? new Dictionary<int, SliderTiming>();

                _isHoldingSlider = false;
                _sliderCharIndex = -1;
                _successfullyCompletedSliders.Clear();
                _successfullyHeldSliders.Clear();

                Console.WriteLine($"[PHASE] \"{CurrentPhaseText}\" started at {currentTime:F2} | visible until {ev.endTime:F2}");
            }
        }

        private void SkipSpacesInPhase()
        {
            while (CurrentCharIndex < CurrentPhaseChars.Length && CurrentPhaseChars[CurrentCharIndex] == ' ')
                CurrentCharIndex++;
        }

        private void TryHandlePhaseTimeout(double currentTime)
        {
            if (currentTime < _nextPhaseStartTime || _phaseEndHandled)
                return;

            _phaseEndHandled = true;

            bool isLastCharSliderHeldSuccessfully =
                _isHoldingSlider &&
                _sliderCharIndex == CurrentPhaseChars.Length - 1 &&
                _successfullyHeldSliders.Contains(_sliderCharIndex);

            _isActivePhase = false;
            _isHoldingSlider = false;
            _sliderCharIndex = -1;

            if (isLastCharSliderHeldSuccessfully)
            {
                CompletedPhases++;
                TotalScore += PointsPerPhase * Combo;
                _successfullyCompletedSliders.Add(_sliderCharIndex);

                Console.WriteLine($"[PHASE COMPLETE FROM HELD SLIDER] (passive, no release) Combo: {Combo}");
                PhaseComplete = true;
            }
            else if (CurrentCharIndex < CurrentPhaseChars.Length)
            {
                FailedPhases++;
                Combo = 0;
                Misses += CurrentPhaseChars.Length - CurrentCharIndex;
                OnComboBreak?.Invoke();
                PhaseComplete = false;
            }
            else
            {
                PhaseComplete = true;
            }

            _currentPhaseIndex++;
            CurrentCharIndex = 0;
        }

        private void HandleOngoingSlider(double currentTime)
        {
            if (!_isHoldingSlider) return;

            var slider = _currentSliders[_sliderCharIndex];

            if (currentTime >= slider.endTime && !_successfullyHeldSliders.Contains(_sliderCharIndex))
            {
                _successfullyHeldSliders.Add(_sliderCharIndex);
                TotalScore += PointsPerHit * Combo * 2;

                Console.WriteLine($"[SLIDER AUTO SUCCESS] '{CurrentPhaseChars[_sliderCharIndex]}' — время вышло, можно отпускать! (now green)");
            }
        }

        private void TryHandleSliderRelease(KeyboardState keyboard, double currentTime)
        {
            if (!_isHoldingSlider || keyboard.IsKeyDown(_heldKey))
                return;

            var slider = _currentSliders[_sliderCharIndex];
            double delta = Math.Abs(currentTime - slider.endTime);

            // Используем глобальные окна из MapData
            double perfectEnd = CurrentMap.GlobalSliderPerfectEndWindow;
            double goodEnd = CurrentMap.GlobalSliderGoodEndWindow;

            string judgement;
            bool isSuccess = false;

            if (delta <= perfectEnd)
            {
                TotalScore += PointsPerHit * Combo * 2;
                judgement = "PERFECT";
                isSuccess = true;
            }
            else if (delta <= goodEnd)
            {
                TotalScore += PointsPerHit * Combo;
                judgement = "GOOD";
                isSuccess = true;
            }
            else
            {
                Combo = 0;
                judgement = "MISS";
                OnComboBreak?.Invoke();
                isSuccess = false;
            }

            Console.WriteLine($"[SLIDER RELEASE] '{CurrentPhaseChars[_sliderCharIndex]}' delta={delta:F3}s → {judgement}");

            if (isSuccess)
            {
                _successfullyCompletedSliders.Add(_sliderCharIndex);

                if (_sliderCharIndex == CurrentPhaseChars.Length - 1)
                    CompletePhase();
                else
                    CurrentCharIndex = _sliderCharIndex + 1;
            }
            else
            {
                CurrentCharIndex = _sliderCharIndex + 1;
            }

            _isHoldingSlider = false;
            _sliderCharIndex = -1;
        }

        private bool TryStartSlider(char inputChar, char expected, double currentTime)
        {
            if (!_currentSliders.TryGetValue(CurrentCharIndex, out var slider))
                return false;

            if (_isHoldingSlider && _sliderCharIndex == CurrentCharIndex)
                return true;

            // Используем глобальные окна из MapData
            double goodStartWindow = CurrentMap.GlobalSliderGoodStartWindow;

            double delta = Math.Abs(currentTime - slider.startTime);

            if (delta <= goodStartWindow)
            {
                if (!_isHoldingSlider)
                {
                    _isHoldingSlider = true;
                    _sliderCharIndex = CurrentCharIndex;
                    if (!CharToKeyMap.TryGetValue(inputChar, out _heldKey))
                    {
                        // Клавиша не поддерживается нашей раскладкой — игнорируем
                        Console.WriteLine($"[INPUT] Unsupported key for char '{inputChar}'");
                        return false;   // или true, если хочешь полностью съесть событие
                    }

                    Console.WriteLine($"[SLIDER START] '{expected}' (index {CurrentCharIndex}) at {currentTime:F3}s | Hold the key! delta={delta:F3}");
                }
                return true;
            }

            Console.WriteLine($"[SLIDER BAD TIMING] '{expected}' delta={delta:F3}s (goodWindow={goodStartWindow})");

            Combo = 0;
            OnComboBreak?.Invoke();
            Misses++;

            if (CurrentCharIndex == CurrentPhaseChars.Length - 1)
            {
                CompletedPhases++;
                TotalScore += PointsPerPhase * Combo;
                PhaseComplete = true;
                _isActivePhase = false;
                _currentPhaseIndex++;
                CurrentCharIndex = 0;

                Console.WriteLine($"[PHASE COMPLETE - BAD SLIDER but pressed] Combo reset");
                return true;
            }

            CurrentCharIndex++;
            return true;
        }

        private void HandleRegularTap(char inputChar, char expected)
        {
            if (inputChar != expected)
            {
                Combo = 0;
                OnComboBreak?.Invoke();
                return;
            }

            CurrentCharIndex++;
            CorrectHits++;
            Combo++;
            OnComboChanged?.Invoke(Combo);
            if (Combo > MaxCombo) MaxCombo = Combo;
            TotalScore += PointsPerHit * Combo;

            Console.WriteLine($"[TAP] '{expected}' OK");

            if (CurrentCharIndex >= CurrentPhaseChars.Length)
                CompletePhase();
        }

        private void CompletePhase()
        {
            PhaseComplete = true;
            _isActivePhase = false;
            _isHoldingSlider = false;
            _sliderCharIndex = -1;

            CompletedPhases++;
            TotalScore += PointsPerPhase * Combo;

            _currentPhaseIndex++;
            CurrentCharIndex = 0;

            Console.WriteLine($"[PHASE COMPLETE] Combo: {Combo}");
        }
    }
}