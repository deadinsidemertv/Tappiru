namespace TappiruCS.GameLogic.Logic
{
    public class ScoringSystem
    {
        // ── Public state ──────────────────────────────────────────────────────────
        public float TotalScore { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int CorrectHits { get; private set; }
        public int Misses { get; internal set; }   // internal: PhaseManager может писать напрямую
        public float Accuracy { get; private set; } = 100f;

        public int PerfectSliders { get; private set; }
        public int GoodSliders { get; private set; }

        // Множитель от модов, устанавливается снаружи один раз при создании сессии
        public float ScoreMultiplier { get; set; } = 1f;

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<int> OnComboChanged;
        public event Action OnComboBreak;

        // ── Private helpers ───────────────────────────────────────────────────────
        private float PointsPerHit => 100f * ScoreMultiplier;
        private float PointsPerPhase => 300f * ScoreMultiplier;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Засчитать обычный тап.</summary>
        public void RegisterHit()
        {
            AddHit(gradeMultiplier: 1f);
        }

        /// <summary>
        /// Засчитать успешный слайдер.
        /// <param name="gradeMultiplier">1.0 = Perfect, 0.5 = Good.</param>
        /// </summary>
        public void RegisterSliderHit(float gradeMultiplier)
        {
            if (gradeMultiplier >= 1f)
                PerfectSliders++;
            else
                GoodSliders++;

            AddHit(gradeMultiplier);
        }

        /// <summary>Засчитать промах (обычный тап или слайдер).</summary>
        public void RegisterMiss()
        {
            Misses++;
            Combo = 0;
            OnComboBreak?.Invoke();
            RecalcAccuracy();
        }

        /// <summary>Сбросить комбо без увеличения счётчика промахов.</summary>
        public void BreakCombo()
        {
            Combo = 0;
            OnComboBreak?.Invoke();
        }

        /// <summary>Бонус за завершение фазы — берёт текущее комбо на момент вызова.</summary>
        public void RegisterPhaseComplete()
        {
            TotalScore += PointsPerPhase * Combo;
        }

        public void Reset()
        {
            TotalScore = Combo = MaxCombo = CorrectHits = Misses = 0;
            PerfectSliders = GoodSliders = 0;
            Accuracy = 100f;
        }

        // ── Private ───────────────────────────────────────────────────────────────
        private void AddHit(float gradeMultiplier)
        {
            CorrectHits++;
            Combo++;
            if (Combo > MaxCombo)
                MaxCombo = Combo;

            TotalScore += PointsPerHit * Combo * gradeMultiplier;

            OnComboChanged?.Invoke(Combo);
            RecalcAccuracy();
        }

        private void RecalcAccuracy()
        {
            int total = CorrectHits + Misses;
            Accuracy = total > 0 ? CorrectHits / (float)total * 100f : 100f;
        }
    }
}