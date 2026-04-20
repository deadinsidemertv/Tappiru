namespace TappiruCS.GameLogic.Logic
{
    public class ScoringSystem
    {
        public int TotalScore { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int CorrectHits { get; private set; }
        public int Misses { get; internal set; }        // internal set — нужно для PhaseManager
        public float Accuracy { get; private set; } = 100f;

        private const int PointsPerHit = 100;
        private const int PointsPerPhase = 300;

        public event Action<int> OnComboChanged;
        public event Action OnComboBreak;

        public void Hit()
        {
            CorrectHits++;
            Combo++;
            OnComboChanged?.Invoke(Combo);
            if (Combo > MaxCombo) MaxCombo = Combo;
            TotalScore += PointsPerHit * Combo;
            UpdateAccuracy();
        }

        public void Miss()
        {
            Misses++;
            Combo = 0;
            OnComboBreak?.Invoke();
            UpdateAccuracy();
        }

        public void PhaseComplete()
        {
            TotalScore += PointsPerPhase * Combo;
        }

        public void ResetCombo()
        {
            Combo = 0;
            OnComboBreak?.Invoke();
        }

        private void UpdateAccuracy()
        {
            Accuracy = (CorrectHits + Misses) > 0
                ? CorrectHits / (float)(CorrectHits + Misses) * 100f
                : 100f;
        }

        public void Reset()
        {
            TotalScore = Combo = MaxCombo = CorrectHits = Misses = 0;
            Accuracy = 100f;
        }
    }
}