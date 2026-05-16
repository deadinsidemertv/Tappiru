using TappiruCS.Core.GameObject;
using System;

namespace TappiruCS.Tween
{
    /// <summary>
    /// Постоянно живущий tween, который следит за состоянием hover
    /// </summary>
    public class HoverOpacityTween : BaseTween
    {
        private readonly Func<bool> _isHovered;
        private float _currentTarget = 0.7f; // начальное значение (неактивно)

        public HoverOpacityTween(GameObject target, Func<bool> isHovered, float duration = 0.2f)
            : base(target)
        {
            _isHovered = isHovered;
        }

        public override void Update(double dt)
        {
            float desiredOpacity = _isHovered() ? 1.0f : 0f;   // ← можешь изменить 0.7f на нужное тебе значение

            // Если цель изменилась — запускаем короткую анимацию
            if (Math.Abs(desiredOpacity - _currentTarget) > 0.01f)
            {
                _currentTarget = desiredOpacity;
                Target.TweenOpacity(desiredOpacity, 0.25f);   // короткая плавная анимация
            }
        }
    }
}