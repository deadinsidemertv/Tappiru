using OpenTK.Mathematics;

namespace TappiruCS.Core.Tween.Animations
{
    public class ScaleAnim : BaseTween
    {
        private readonly float _startScale;
        private readonly float _endScale;

        public ScaleAnim(GameObject target, float multiplier, float duration = 0.2f)
            : base(target, duration)
        {
            // Если базовый масштаб ещё не был сохранён — сохраняем ТЕКУЩЕЕ значение ScaleMultiply
            if (target._baseScaleMultiply < 0f)
                target._baseScaleMultiply = target.ScaleMultiply;   // ← вот здесь было главное

            _startScale = target.ScaleMultiply;                    // ← начинаем от текущего состояния
            _endScale = target._baseScaleMultiply * multiplier;    // ← цель относительно базового
        }

        public override void Update(double deltaTime)
        {
            if (IsFinished)
            {
                Target.ScaleMultiply = _endScale;   // принудительно ставим конечное значение
                return;
            }

            _time += (float)deltaTime;
            float progress = Math.Clamp(_time / Duration, 0f, 1f);

            Target.ScaleMultiply = MathHelper.Lerp(_startScale, _endScale, progress);
        }
    }
}