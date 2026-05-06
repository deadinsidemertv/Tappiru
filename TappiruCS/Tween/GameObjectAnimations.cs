using TappiruCS.Core.GameObject;

namespace TappiruCS.Tween
{
    public static class GameObjectAnimations
    {
        public static void AddHoverOpacity(this GameObject obj, Func<bool> isHovered, float duration = 0.2f)
        {
            var anim = new OpacityHoverAnim(obj, isHovered, duration);
            Scene.Current.TweenManager.Add(anim);
        }
    }
}