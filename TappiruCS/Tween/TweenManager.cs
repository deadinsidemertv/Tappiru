namespace TappiruCS.Core.Tween
{
    public class TweenManager
    {
        private readonly List<BaseTween> _activeTweens = new List<BaseTween>();

        public BaseTween Add(BaseTween tween)
        {
            if (tween != null)
                _activeTweens.Add(tween);
            return tween;
        }

        public void Update(double deltaTime)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tween = _activeTweens[i];
                tween.Update(deltaTime);

                if (tween.IsFinished)
                    _activeTweens.RemoveAt(i);
            }
        }

        public void RemoveAllFor(GameObject target)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                if (_activeTweens[i].Target == target)
                    _activeTweens.RemoveAt(i);
            }
        }

        public void Clear() => _activeTweens.Clear();
    }
}