using System;
using System.Collections.Generic;

namespace TappiruCS.GameLogic
{
    public class GameSession
    {
        //-------------Гейм механика--------------------//
        public MapData CurrentMap { get; }
        private int _currentPhaseIndex;          // индекс текущей строки (фазы)
        private bool _isActivePhase;             // активна ли фаза (игрок вводит)
        private bool _phaseEndHandled;           // флаг, чтобы не начислять штраф несколько раз
        private double _nextPhaseStartTime;      // время начала следующей фазы (или бесконечность)
        public double endTime { get; private set; }

        public string CurrentPhaseText { get; private set; }   // текущая строка
        public char[] CurrentPhaseChars { get; private set; }  // строка разбитая на символы
        public int CurrentCharIndex { get; private set; }      // индекс символа, который нужно нажать
        public bool PhaseComplete { get; private set; }        // флаг успешного завершения фазы

        //------------Игровые показатели---------------//
        public int TotalScore { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }              // максимальное достигнутое комбо
        public float Health { get; private set; }

        // Статистика для скорборда
        public int CorrectHits { get; private set; }           // правильные нажатия (символы)
        public int Misses { get; private set; }                // неправильные нажатия (символы)
        public int CompletedPhases { get; private set; }       // успешно завершённые строки
        public int FailedPhases { get; private set; }          // проваленные строки (по таймауту)
        public int TotalNotes { get; private set; }            // общее количество символов в карте

        public float Accuracy;

        // Константы очков
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

            _currentPhaseIndex = 0;
            _isActivePhase = false;
            _phaseEndHandled = false;
            _nextPhaseStartTime = double.PositiveInfinity;

            // Подсчёт общего количества символов
            TotalNotes = 0;
            foreach (var ev in CurrentMap.Events)
                TotalNotes += ev.text.Length;
            Console.WriteLine(TotalNotes);
            Accuracy = (1 - (float)Misses / TotalNotes) * 100;
            

        }

        public void Update(double currentTime)
        {
            Accuracy = (1 - (float)Misses / TotalNotes)*100;
            Console.WriteLine(Accuracy);
            // 1. Активация новой фазы, если не активна и есть ещё события
            if (!_isActivePhase && _currentPhaseIndex < CurrentMap.Events.Count)
            {
                var ev = CurrentMap.Events[_currentPhaseIndex];
                // Определяем время окончания текущей фазы (начало следующей или бесконечность)
                if (_currentPhaseIndex + 1 < CurrentMap.Events.Count)
                    _nextPhaseStartTime = CurrentMap.Events[_currentPhaseIndex + 1].time;
                else
                    _nextPhaseStartTime = double.PositiveInfinity;

                if (currentTime >= ev.time && currentTime < _nextPhaseStartTime)
                {
                    // Активируем фазу
                    CurrentPhaseText = ev.text;
                    CurrentPhaseChars = CurrentPhaseText.ToCharArray();
                    CurrentCharIndex = 0;
                    _isActivePhase = true;
                    PhaseComplete = false;
                    _phaseEndHandled = false;   // сбрасываем флаг завершения
                }
            }

            // 2. Принудительное завершение фазы по таймауту (наступило время следующей фазы)
            if (_isActivePhase && currentTime >= _nextPhaseStartTime && !_phaseEndHandled)
            {
                _phaseEndHandled = true;
                _isActivePhase = false;

                // Если не все символы введены – фаза провалена
                if (CurrentCharIndex != CurrentPhaseChars.Length)
                {
                    FailedPhases++;
                    // Штраф: комбо сбрасывается, Misses увеличивается на количество пропущенных символов
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
            if (!_isActivePhase) return;

            if (inputChar == CurrentPhaseChars[CurrentCharIndex])
            {
                // Правильное нажатие
                CurrentCharIndex++;
                CorrectHits++;
                Combo++;
                if (Combo > MaxCombo) MaxCombo = Combo;

                // Начисление очков: 100 * комбо
                TotalScore += PointsPerHit * Combo;

                // Фаза полностью завершена?
                if (CurrentCharIndex == CurrentPhaseChars.Length)
                {
                    PhaseComplete = true;
                    _isActivePhase = false;
                    CompletedPhases++;

                    // Бонус за завершённую линию: 300 * комбо
                    TotalScore += PointsPerPhase * Combo;

                    _currentPhaseIndex++;
                    CurrentCharIndex = 0;
                }
            }
            else
            {
                // Неправильное нажатие
                Console.WriteLine($"Ошибка: ожидался '{CurrentPhaseChars[CurrentCharIndex]}', получен '{inputChar}'");
                Misses++;
                Combo = 0;
                // Здесь можно добавить штраф здоровью, если нужно
                // Health -= 10f;
            }
        }

        /// <summary>
        /// Вывод всех результатов в консоль (для последующей вставки в ScoreBoard)
        /// </summary>
        public void PrintResults()
        {
            Console.WriteLine("===== РЕЗУЛЬТАТЫ КАРТЫ =====");
            //Console.WriteLine($"Название: {CurrentMap.title ?? "Не указано"}");
            //Console.WriteLine($"Сложность: {CurrentMap.difficulty ?? "Не указана"}");
            Console.WriteLine($"Итоговый счёт: {TotalScore}");
            Console.WriteLine($"Максимальное комбо: {MaxCombo}");
            Console.WriteLine($"Правильных нажатий: {CorrectHits}");
            Console.WriteLine($"Ошибок (мисов): {Misses}");
            Console.WriteLine($"Всего символов: {TotalNotes}");
            Console.WriteLine($"Точность: {Accuracy:F2}%");
            Console.WriteLine($"Успешно завершённых строк: {CompletedPhases}");
            Console.WriteLine($"Проваленных строк: {FailedPhases}");
            Console.WriteLine($"Оставшееся здоровье: {Health:F1}");
            Console.WriteLine("============================");
        }
    }
}