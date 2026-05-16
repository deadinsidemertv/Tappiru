using OpenTK.Mathematics;

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
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                var tween = _tweens[i];
                tween.Update(dt);

                // Удаляем завершённые твины, чтобы не копились в памяти
                if (tween is Tween<float> ft && ft.IsCompleted ||
                    tween is Tween<Vector2> vt && vt.IsCompleted)
                {
                    _tweens.RemoveAt(i);
                }
            }
        }
    }
}