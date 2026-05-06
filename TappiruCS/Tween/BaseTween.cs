using TappiruCS.Core.GameObject;

namespace TappiruCS.Tween
{
    public abstract class BaseTween
    {
        public readonly GameObject Target;

        protected BaseTween(GameObject target)
        {
            Target = target;
        }

        public abstract void Update(double dt);
    }
}