using TappiruCS.Core.GameObject;

namespace TappiruCS.Tween
{
    public abstract class BaseTween
    {
        public readonly GameObject Target;   //На какой объект работаем
        public readonly float Duration;         //длительность
        protected float _time = 0f;           //конкретное время 

        public bool IsFinished => _time >= Duration;         //считается завершенное когда время > длительности

        protected BaseTween(GameObject target, float duration)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Duration = Math.Max(duration, 0.01f);
        }

        public abstract void Update(double deltaTime);

        // Можно будет расширять позже
        public virtual void OnStart() { }
        public virtual void OnComplete() { }
    }
}