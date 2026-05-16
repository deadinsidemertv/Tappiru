using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TappiruCS.UI.Sprite
{
    public class NineSliceSprite : SpriteObject
    {
        // Отступы задаются в ПИКСЕЛЯХ ТЕКСТУРЫ (не в экранных пикселях, не в scale).
        // Например если угол занимает 12px в текстуре 502x97 — ставишь 12.
        // X = left, Y = right, Z = top, W = bottom
        public Vector4 SliceBorders { get; set; } = new Vector4(12, 12, 12, 12);

        public float SliceLeft => SliceBorders.X;
        public float SliceTop => SliceBorders.Z;
        public float SliceRight => SliceBorders.Y;
        public float SliceBottom => SliceBorders.W;

        public NineSliceSprite(int texture, float x, float y, float width, float height)
            : base(texture, x, y, width, height)
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

            // SliceBorders заданы в пикселях ТЕКСТУРЫ.
            // Переводим их в экранные пиксели пропорционально растяжению:
            //   texPx / texSize * drawSize
            // Пример: текстура 502x97, drawW=200, left=12
            //   → 12/502 * 200 = ~4.8px на экране
            // Это работает при ЛЮБОМ размере спрайта.
            float texW = SB.GetTextureWidth(_textureId);
            float texH = SB.GetTextureHeight(_textureId);

            Vector4 screenBorders = new Vector4(
                    SliceBorders.X * ScaleMultiply * texW,   // left
                    SliceBorders.Y * ScaleMultiply * texW,   // right  
                    SliceBorders.Z * ScaleMultiply * texH,   // top
                    SliceBorders.W * ScaleMultiply * texH    // bottom
                    );

            if (EnableGlow && GlowIntensity > 0.01f)
                SB.DrawGlowRect(drawX, drawY, drawW, drawH, Color, Opacity,
                                projection, GlowSteps, GlowSpread);

            SB.Draw9Slice(_textureId, drawX, drawY, drawW, drawH,
                          screenBorders, Color, projection);

            // Рисуем только детей — минуя SpriteObject.Draw
            // чтобы он не нарисовал обычный квад поверх NineSlice
            foreach (var child in Children)
                if (child.Active)
                    child.Draw(projection);

            
        }
    }
}