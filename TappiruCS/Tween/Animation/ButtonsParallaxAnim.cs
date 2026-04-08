using TappiruCS.Core;
using TappiruCS.Core.Tween;
using TappiruCS.UI;

namespace TappiruCS.Tween.Animation
{
    public class ButtonsParallaxAnim : BaseTween
    {
        public float _startPosTarger;
        public float _endPosTarget;

        public float _startPosOther;
        public float _endPosOther;

        public List<Button> otherButtons;
        public Button TargetButton;
        public ButtonsParallaxAnim(GameObject target,float duration,List<Button> list) : base(target, duration)
        {
             
        }
        public override void Update(double deltaTime)
        {
            if (IsFinished)
            {
                  
                return;
            }

            _time += (float)deltaTime;
            float progress = Math.Clamp(_time / Duration, 0f, 1f);

            //Target.ScaleMultiply = MathHelper.Lerp(_startScale, _endScale, progress);
        }
    }
}
