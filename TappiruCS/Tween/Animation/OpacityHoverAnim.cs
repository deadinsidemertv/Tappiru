using Gtk;
using TappiruCS.Core.GameObject;
using TappiruCS.Tween;

public class OpacityHoverAnim : BaseTween
{
    private readonly Func<bool> _isHovered;

    private float _current;
    private float _start;
    private float _target;

    private float _time;
    private float _duration;

    public OpacityHoverAnim(GameObject target, Func<bool> isHovered, float duration = 0.2f)
        : base(target)
    {
        _isHovered = isHovered;

        _current = target.Opacity;
        _start = _current;
        _target = _current;

        _duration = duration;
    }

    public override void Update(double dt)
    {
        float newTarget = _isHovered() ? 1f : 0f;

        // 👉 если цель изменилась — перезапускаем анимацию
        if (newTarget != _target)
        {
            _start = _current;
            _target = newTarget;
            _time = 0f;
        }

        _time += (float)dt;

        float t = Math.Clamp(_time / _duration, 0f, 1f);

        // easing
        t = 1 - MathF.Pow(1 - t, 3);

        // 👉 линейная интерполяция
        _current = _start + (_target - _start) * t;

        Target.Opacity = _current;
    }
}