namespace TappiruCS.GameLogic.Logic
{
    public class HealthSystem
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const float HpGainTap = 3.5f;
        private const float HpGainSliderPerfect = 6f;
        private const float HpGainSliderGood = 4f;
        private const float HpGainPhase = 8f;
        private const float HpMiss = 7f;
        private const float HpPhaseFailPerChar = 5.5f;

        // ── State ─────────────────────────────────────────────────────────────────
        public float Health { get; private set; } = 100f;

        private readonly GameSession _session;

        // ── Constructor ───────────────────────────────────────────────────────────
        public HealthSystem(GameSession session)
        {
            _session = session;
        }

        // ── Public API ────────────────────────────────────────────────────────────
        public void GainTap() => Adjust(+HpGainTap);
        public void GainSliderPerfect() => Adjust(+HpGainSliderPerfect);
        public void GainSliderGood() => Adjust(+HpGainSliderGood);
        public void GainPhaseComplete() => Adjust(+HpGainPhase);
        public void Miss() => Adjust(-HpMiss);
        public void PhaseFail(int remaining) => Adjust(-HpPhaseFailPerChar * remaining);

        public void Reset() => Health = 100f;

        // ── Private ───────────────────────────────────────────────────────────────
        private void Adjust(float delta)
        {
            if (_session.NoFail)
                return;

            Health = Math.Clamp(Health + delta, 0f, 100f);
        }
    }
}