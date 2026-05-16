using System;

namespace TappiruCS.Tween
{
    public static class Easing
    {
        public static float OutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
        public static float OutBack(float t)
        {
            const float c = 1.70158f;
            return 1 + c * MathF.Pow(t - 1, 3) + c * MathF.Pow(t - 1, 2);
        }

        public static float OutElastic(float t)
        {
            if (t == 0 || t == 1) return t;
            return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * (MathF.PI * 2) / 3) + 1;
        }

        public static float InOutCubic(float t) => t < 0.5f
            ? 4 * t * t * t
            : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
    }
}