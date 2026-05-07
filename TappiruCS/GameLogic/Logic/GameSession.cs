using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using TappiruCS.GameLogic.Mod;

namespace TappiruCS.GameLogic.Logic
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
        public double EndTime { get; private set; }                    // было endTime

        // Публичные свойства — полностью совместимы со старым кодом
        public double CurrentPhaseStartTime => _phaseManager.CurrentPhaseStartTime;
        public double CurrentPhaseEndTime => _phaseManager.CurrentPhaseEndTime;


        public string CurrentPhaseText => _phaseManager.CurrentPhaseText;
        public char[] CurrentPhaseChars => _phaseManager.CurrentPhaseChars;

        public string CurrentPhaseDisplayText => _phaseManager.CurrentPhaseDisplayText;
        public char[] CurrentPhaseDisplayChars => _phaseManager.CurrentPhaseDisplayChar;

        public int CurrentCharIndex => _phaseManager.CurrentCharIndex;
        public bool PhaseComplete => _phaseManager.PhaseComplete;

        public bool IsActivePhase => _phaseManager.IsActivePhase;

        public Dictionary<int, SliderTiming> CurrentSliders => _sliderManager.CurrentSliders;
        public bool IsHoldingSlider => _sliderManager.IsHoldingSlider;
        public int CurrentSliderCharIndex => _sliderManager.CurrentSliderCharIndex;

        public float TotalScore => _scoring.TotalScore;
        public int Combo => _scoring.Combo;
        public int MaxCombo => _scoring.MaxCombo;

        public int CorrectHits => _scoring.CorrectHits;
        public int Misses => _scoring.Misses;
        public int CompletedPhases => _phaseManager.CompletedPhases;
        public int FailedPhases => _phaseManager.FailedPhases;

        public int PerfectSliders => _scoring.PerfectSliders;
        public int GoodSliders => _scoring.GoodSliders;

        public float Accuracy => _scoring.Accuracy;
        public float Health => _healthSystem.Health;        // было healt

        public bool IsMapCompleted => _phaseManager.IsMapCompleted;

        public HashSet<int> SuccessfullyCompletedSliders => _sliderManager.SuccessfullyCompletedSliders as HashSet<int>;
        public HashSet<int> SuccessfullyHeldSliders => _sliderManager.SuccessfullyHeldSliders as HashSet<int>;

        private readonly PhaseManager _phaseManager;
        private readonly SliderManager _sliderManager;
        private readonly ScoringSystem _scoring;
        private readonly HealthSystem _healthSystem;

        public event Action<int> OnComboChanged;
        public event Action OnComboBreak;

        public bool IsPause { get; set; }

        //Mods//
        public bool NoFail { get; set; } = false;
        public GameSession(MapData mapData)
        {

            CurrentMap = mapData;
            EndTime = mapData.endTime;


            // ★★★ Расчёт общего множителя очков из модов ★★★
            float totalMultiplier = 1f;
            if (mapData.mods != null)
            {
                foreach (var mod in mapData.mods)
                    totalMultiplier *= mod.ScoreMultiply;
            }

            _scoring = new ScoringSystem();
            _scoring.ScoreMultiplier = totalMultiplier;



            _healthSystem = new HealthSystem(this);
            _sliderManager = new SliderManager(mapData, CharToKeyMap);
            _phaseManager = new PhaseManager(mapData, _scoring, _healthSystem, _sliderManager);

            _scoring.OnComboChanged += c => OnComboChanged?.Invoke(c);
            _scoring.OnComboBreak += () => OnComboBreak?.Invoke();

            _sliderManager.OnSliderHit += multiplier => _scoring.RegisterSliderHit(multiplier);

            NoFail = mapData.mods.Any(m => m.GetType() == typeof(NoFailMod));
        }

        public void Update(double currentTime, KeyboardState keyboard)
        {
            if (IsPause) return;

            _phaseManager.Update(currentTime);
            _sliderManager.Update(currentTime, keyboard);
        }

        public void HandleInput(char inputChar, double currentTime)
        {
            if (IsPause)
                return;

            if (!IsInputAllowed(currentTime)) return;
            _phaseManager.HandleInput(inputChar, currentTime);
        }

        public bool IsInputAllowed(double currentTime)          // ← публичный метод для совместимости
        {
            return _phaseManager.IsInputAllowed(currentTime);
        }

        public void Reset()
        {
            _scoring.Reset();
            _healthSystem.Reset();
            _sliderManager.Reset();
            _phaseManager.Reset();
            IsPause = false;
        }
    }
}