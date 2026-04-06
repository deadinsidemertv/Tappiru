using OpenTK.Mathematics;
using System;

namespace TappiruCS.Core.TappiruCS.Core
{
    public enum TweenType
    {
        ScaleMultiply,
        // Position, Color, Alpha и т.д. можно будет добавить позже
    }

    public class Tween
    {
        public readonly TweenType Type;
        private readonly GameObject _target;

        private float _startValue;
        private float _endValue;
        private readonly float _duration;
        private float _time = 0f;

        public bool IsFinished => _time >= _duration;

        public Tween(GameObject target, TweenType type, float endValue, float duration)
        {
            _target = target;
            Type = type;
            _endValue = endValue;
            _duration = Math.Max(duration, 0.01f);

            _startValue = type switch
            {
                TweenType.ScaleMultiply => target.ScaleMultiply,
                _ => 1f
            };
        }

        public void Update(double deltaTime)
        {
            if (IsFinished) return;

            _time += (float)deltaTime;
            float progress = Math.Clamp(_time / _duration, 0f, 1f);

            float current = MathHelper.Lerp(_startValue, _endValue, progress);

            if (Type == TweenType.ScaleMultiply)
                _target.ScaleMultiply = current;
        }
    }
}