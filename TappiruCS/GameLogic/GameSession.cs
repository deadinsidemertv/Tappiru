using System;
using System.Collections.Generic;

namespace TappiruCS.GameLogic
{
    public class GameSession
    {
        //-------------Гейм механика--------------------//
        public MapData CurrentMap { get; }
        private int _currentPhaseIndex;
        public bool _isActivePhase;
        private bool _phaseEndHandled;
        private double _nextPhaseStartTime;
        public double endTime { get; private set; }

        public string CurrentPhaseText { get; private set; }
        public char[] CurrentPhaseChars { get; private set; }
        public int CurrentCharIndex { get; private set; }
        public bool PhaseComplete { get; private set; }

        // Новое поле — карта полностью завершена (последняя фраза введена)
        public bool IsMapCompleted { get; private set; }

        //------------Игровые показатели---------------//
        public int TotalScore { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public float Health { get; private set; }

        public int CorrectHits { get; private set; }
        public int Misses { get; private set; }
        public int CompletedPhases { get; private set; }
        public int FailedPhases { get; private set; }
        public int TotalNotes { get; private set; }

        public float Accuracy;

        private const int PointsPerHit = 100;
        private const int PointsPerPhase = 300;

        public GameSession(MapData mapData)
        {
            CurrentMap = mapData ?? throw new ArgumentNullException(nameof(mapData));
            endTime = mapData.endTime;

            TotalScore = 0;
            Combo = 0;
            MaxCombo = 0;
            Health = 100f;

            CorrectHits = 0;
            Misses = 0;
            CompletedPhases = 0;
            FailedPhases = 0;
            IsMapCompleted = false;           // ← новое

            _currentPhaseIndex = 0;
            _isActivePhase = false;
            _phaseEndHandled = false;
            _nextPhaseStartTime = double.PositiveInfinity;

            TotalNotes = 0;
            foreach (var ev in CurrentMap.Events)
                TotalNotes += ev.text.Length;

            Accuracy = 100f;
        }

        public void Update(double currentTime)
        {
            Accuracy = (CorrectHits + Misses) > 0
                ? (CorrectHits / (float)(CorrectHits + Misses)) * 100f
                : 100f;
            // Если карта уже завершена — больше ничего не делаем (ждём конца музыки)
            if (IsMapCompleted)
                return;

            // 1. Активация новой фазы
            if (!_isActivePhase && _currentPhaseIndex < CurrentMap.Events.Count)
            {
                var ev = CurrentMap.Events[_currentPhaseIndex];

                if (_currentPhaseIndex + 1 < CurrentMap.Events.Count)
                    _nextPhaseStartTime = CurrentMap.Events[_currentPhaseIndex + 1].time;
                else
                    _nextPhaseStartTime = double.PositiveInfinity;

                if (currentTime >= ev.time && currentTime < _nextPhaseStartTime)
                {
                    CurrentPhaseText = ev.text;
                    CurrentPhaseChars = CurrentPhaseText.ToCharArray();
                    CurrentCharIndex = 0;
                    _isActivePhase = true;
                    PhaseComplete = false;
                    _phaseEndHandled = false;
                }
            }

            // 2. Принудительное завершение по таймауту
            if (_isActivePhase && currentTime >= _nextPhaseStartTime && !_phaseEndHandled)
            {
                _phaseEndHandled = true;
                _isActivePhase = false;

                if (CurrentCharIndex != CurrentPhaseChars.Length)
                {
                    FailedPhases++;
                    Combo = 0;
                    Misses += CurrentPhaseChars.Length - CurrentCharIndex;
                }

                _currentPhaseIndex++;
                CurrentCharIndex = 0;
                PhaseComplete = false;
            }
        }

        public void HandleInput(char inputChar)
        {
            // Блокируем ввод после завершения последней фразы
            if (!_isActivePhase || IsMapCompleted) return;

            if (inputChar == CurrentPhaseChars[CurrentCharIndex])
            {
                CurrentCharIndex++;
                CorrectHits++;
                Combo++;
                if (Combo > MaxCombo) MaxCombo = Combo;

                TotalScore += PointsPerHit * Combo;

                // Фраза полностью завершена?
                if (CurrentCharIndex == CurrentPhaseChars.Length)
                {
                    PhaseComplete = true;
                    _isActivePhase = false;
                    CompletedPhases++;

                    TotalScore += PointsPerPhase * Combo;

                    // === ИСПРАВЛЕНИЕ БАГА ===
                    if (_currentPhaseIndex + 1 >= CurrentMap.Events.Count)
                    {
                        // Это была ПОСЛЕДНЯЯ фраза
                        IsMapCompleted = true;
                        // CurrentCharIndex НЕ сбрасываем → фраза остаётся полностью подсвеченной
                    }
                    else
                    {
                        _currentPhaseIndex++;
                        CurrentCharIndex = 0;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Ошибка: ожидался '{CurrentPhaseChars[CurrentCharIndex]}', получен '{inputChar}'");
                Combo = 0;
            }
        }

        public void PrintResults()
        {
            Console.WriteLine("===== РЕЗУЛЬТАТЫ КАРТЫ =====");
            Console.WriteLine($"Итоговый счёт: {TotalScore}");
            Console.WriteLine($"Максимальное комбо: {MaxCombo}");
            Console.WriteLine($"Правильных нажатий: {CorrectHits}");
            Console.WriteLine($"Ошибок (мисов): {Misses}");
            Console.WriteLine($"Всего символов: {TotalNotes}");
            Console.WriteLine($"Точность: {Accuracy:F2}%");
            Console.WriteLine($"Успешно завершённых строк: {CompletedPhases}");
            Console.WriteLine($"Проваленных строк: {FailedPhases}");
            Console.WriteLine($"Карта завершена: {IsMapCompleted}");
            Console.WriteLine($"Оставшееся здоровье: {Health:F1}");
            Console.WriteLine("============================");
        }
    }
}