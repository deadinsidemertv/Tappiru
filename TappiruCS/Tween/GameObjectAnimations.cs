using TappiruCS.Core.GameObject;

namespace TappiruCS.Tween
{
    public static class GameObjectAnimations
    {
        public static void AddHoverOpacity(this GameObject obj, Func<bool> isHovered, float duration = 0.2f)
        {
            var hover = new HoverOpacityTween(obj, isHovered, duration);
            Scene.Current.TweenManager.Add(hover);
        }
    }
}