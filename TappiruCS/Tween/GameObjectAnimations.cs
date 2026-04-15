using TappiruCS.Core.GameObject;
using TappiruCS.Tween.Animation;

namespace TappiruCS.Tween 
{
public static class GameObjectAnimations
{
    public static BaseTween AnimScale(this GameObject obj, float multiplier, float duration = 0.2f)
    {
        if (Scene.Current == null) return null;

        // Убираем ВСЕ предыдущие анимации этого объекта
        Scene.Current.TweenManager.RemoveAllFor(obj);

        var tween = new ScaleAnim(obj, multiplier, duration);
        Scene.Current.TweenManager.Add(tween);
        return tween;
    }

    public static BaseTween AnimScaleReset(this GameObject obj, float duration = 0.22f)
    {
        if (Scene.Current == null) return null;

        Scene.Current.TweenManager.RemoveAllFor(obj);

        var tween = new ScaleAnim(obj, 1.0f, duration);
        Scene.Current.TweenManager.Add(tween);
        return tween;
    }

    public static void ClearTweens(this GameObject obj)
    {
        Scene.Current?.TweenManager.RemoveAllFor(obj);
    }
}
}
