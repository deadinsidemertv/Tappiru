using TappiruCS.Core.GameObject;

namespace TappiruCS.Tween
{
    public class TweenManager
    {
        private readonly List<BaseTween> _tweens = new();

        public void Add(BaseTween tween)
        {
            if (tween != null)
                _tweens.Add(tween);
        }

        public void Update(double dt)
        {
            foreach (var t in _tweens)
                t.Update(dt);
        }
    }
}