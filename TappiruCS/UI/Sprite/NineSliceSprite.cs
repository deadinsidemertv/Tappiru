using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.UI.Sprite
{
    public class NineSliceSprite : SpriteObject
    {
        public Vector4 SliceBorders { get; set; } = new Vector4(5, 5, 5, 5);

        public float SliceLeft => SliceBorders.X;
        public float SliceTop => SliceBorders.Z;
        public float SliceRight => SliceBorders.Y;
        public float SliceBottom => SliceBorders.W;
        public NineSliceSprite(int texture, float x, float y, float width, float height) : base(texture, x, y, 1f, 1f)
        {
            LocalPosition = new Vector2(x, y);

            if (Scale != null) // если у GameObject есть свойство Size
                Scale = new Vector2(width, height);
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


            if (EnableGlow && GlowIntensity > 0.01f)
            {
                SB.DrawGlowRect(drawX, drawY, drawW, drawH, Color, Opacity,
                                projection, GlowSteps, GlowSpread);
            }

            SB.Draw9Slice(_textureId, drawX, drawY, drawW, drawH,
                          SliceBorders, Color, projection);   

            foreach (var child in Children)
            {
                if (child != this)
                    child.Draw(projection);
            }
        }
    }
}
