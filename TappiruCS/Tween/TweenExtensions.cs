using TappiruCS.Core.GameObject;
using OpenTK.Mathematics;

namespace TappiruCS.Tween
{
    public static class TweenExtensions
    {
        public static Tween<float> TweenOpacity(this GameObject obj, float endValue, float duration = 0.3f, Func<float, float> easing = null)
        {
            var tween = new Tween<float>(obj, () => obj.Opacity, v => obj.Opacity = v, endValue, duration);
            Scene.Current.TweenManager.Add(tween);
            return tween;
        }

        public static Tween<Vector2> TweenPosition(this GameObject obj, Vector2 endValue, float duration = 0.4f, Func<float, float> easing = null)
        {
            var tween = new Tween<Vector2>(obj, () => obj.LocalPosition, v => obj.LocalPosition = v, endValue, duration);
            Scene.Current.TweenManager.Add(tween);
            return tween;
        }
    }
}