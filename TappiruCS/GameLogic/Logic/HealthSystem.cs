namespace TappiruCS.GameLogic.Logic
{
    public class HealthSystem
    {
        public GameSession session;

        public HealthSystem(GameSession _session)
        {
            session = _session;
        }
        public float Health { get; private set; } = 100f;

        private const float HP_GAIN_TAP = 3.5f;
        private const float HP_GAIN_SLIDER_HOLD = 5f;
        private const float HP_GAIN_SLIDER_PERFECT = 6f;
        private const float HP_GAIN_SLIDER_GOOD = 4f;
        private const float HP_GAIN_PHASE = 8f;
        private const float HP_MISS = 7f;
        private const float HP_PHASE_FAIL_MULTIPLIER = 5.5f;


        public void GainTap() => Adjust(HP_GAIN_TAP);
        public void GainSliderHold() => Adjust(HP_GAIN_SLIDER_HOLD);
        public void GainSliderPerfect() => Adjust(HP_GAIN_SLIDER_PERFECT);
        public void GainSliderGood() => Adjust(HP_GAIN_SLIDER_GOOD);
        public void GainPhaseComplete() => Adjust(HP_GAIN_PHASE);
        public void Miss() => Adjust(-HP_MISS);
        public void PhaseFail(int remaining) => Adjust(-HP_PHASE_FAIL_MULTIPLIER * remaining);

        private void Adjust(float delta)
        {
            if(session.NoFail) return;

            Health = Math.Clamp(Health + delta, 0f, 100f);
        }

        public void Reset() => Health = 100f;
    }
}