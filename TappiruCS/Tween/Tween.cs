using TappiruCS.Core.GameObject;
using System;
using OpenTK.Mathematics;

namespace TappiruCS.Tween
{
    public class Tween<T> : BaseTween
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly T _startValue;
        private readonly T _endValue;
        private float _time;
        private readonly float _duration;
        private readonly Func<float, float> _easing;
        private bool _completed = false;

        public bool IsCompleted => _completed;

        public Tween(GameObject target, Func<T> getter, Action<T> setter, T endValue,
                     float duration = 0.3f, Func<float, float> easing = null)
            : base(target)
        {
            _getter = getter;
            _setter = setter;
            _startValue = getter();
            _endValue = endValue;
            _duration = duration;
            _easing = easing ?? Easing.OutCubic;
        }

        public override void Update(double dt)
        {
            if (_completed) return;

            _time += (float)dt;
            float t = Math.Clamp(_time / _duration, 0f, 1f);
            t = _easing(t);

            T current = Lerp(_startValue, _endValue, t);
            _setter(current);

            if (t >= 1f)
            {
                _completed = true;
                _setter(_endValue); // гарантируем точное конечное значение
            }
        }

        private T Lerp(T a, T b, float t)
        {
            if (typeof(T) == typeof(float))
            {
                float fa = (float)(object)a;
                float fb = (float)(object)b;
                return (T)(object)(fa + (fb - fa) * t);
            }

            if (typeof(T) == typeof(Vector2))
            {
                var va = (Vector2)(object)a;
                var vb = (Vector2)(object)b;
                return (T)(object)new Vector2(
                    va.X + (vb.X - va.X) * t,
                    va.Y + (vb.Y - va.Y) * t
                );
            }

            throw new NotSupportedException($"Lerp не реализован для типа {typeof(T)}");
        }
    }
}