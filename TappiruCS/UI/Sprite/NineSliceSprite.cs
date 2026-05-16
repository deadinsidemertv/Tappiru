using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TappiruCS.UI.Sprite
{
    public class NineSliceSprite : SpriteObject
    {
        // Отступы в дизайн-единицах (пикселях текстуры при scale=1).
        // X = left, Y = right, Z = top, W = bottom
        public Vector4 SliceBorders { get; set; } = new Vector4(12, 12, 12, 12);

        public float SliceLeft => SliceBorders.X;
        public float SliceTop => SliceBorders.Z;
        public float SliceRight => SliceBorders.Y;
        public float SliceBottom => SliceBorders.W;

        public NineSliceSprite(int texture, float x, float y, float width, float height)
            : base(texture, x, y, width, height) // ← передаём реальные width/height, не 1f
        {
            LocalPosition = new Vector2(x, y);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
        }

        public override void Draw(Matrix4 projection)
        {
            if (!Active || SB == null) return;

            var (dLeft, dTop, effW, effH) = GetDesignBounds();

            float drawX = AutoScale ? dLeft * CanvasScale.X : dLeft;
            float drawY = AutoScale ? dTop * CanvasScale.Y : dTop;
            float drawW = AutoScale ? effW * CanvasScale.X : effW;
            float drawH = AutoScale ? effH * CanvasScale.Y : effH;

            float scaleX = AutoScale ? CanvasScale.X : 1f;
            float scaleY = AutoScale ? CanvasScale.Y : 1f;

            Vector4 scaledBorders = new Vector4(
                SliceBorders.X * scaleX,
                SliceBorders.Y * scaleX,
                SliceBorders.Z * scaleY,
                SliceBorders.W * scaleY
            );

            if (EnableGlow && GlowIntensity > 0.01f)
                SB.DrawGlowRect(drawX, drawY, drawW, drawH, Color, Opacity,
                                projection, GlowSteps, GlowSpread);

            SB.Draw9Slice(_textureId, drawX, drawY, drawW, drawH,
                          scaledBorders, Color, projection);

            // ✅ Прыгаем через SpriteObject прямо к GameObject.Draw
            // чтобы нарисовать детей, но НЕ рисовать спрайт второй раз
            foreach (var child in Children)
                if (child.Active)
                    child.Draw(projection);
        }
    }
}