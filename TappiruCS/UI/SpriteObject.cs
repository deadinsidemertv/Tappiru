// UI/SpriteObject.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;

namespace TappiruCS.UI
{
    public class SpriteObject : GameObject
    {
        public int _textureId;
        public Color4 Color { get; set; } = Color4.White;

        // === Glow свойства ===
        public bool EnableGlow { get; set; } = false;
        public float GlowIntensity { get; set; } = 1f;   // сила свечения 0..1
        public float GlowSpread { get; set; } = 12f;         // радиус размытия
        public int GlowSteps { get; set; } = 6;             // количество слоёв

        public float HoverBrightness = 1.5f;
        

        public SpriteObject(int textureId, float x, float y, float scaleX, float scaleY)
        {
            _textureId = textureId;
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(scaleX, scaleY);
            AllowHover = false;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
        }

        public override void Draw(Matrix4 projection)
        {
            if (!Active || Context == null || SB == null)
                return;

            

            var (dLeft, dTop, effW, effH) = GetDesignBounds();

            float drawX = AutoScale ? dLeft * CanvasScale.X : dLeft;
            float drawY = AutoScale ? dTop * CanvasScale.Y : dTop;
            float drawW = AutoScale ? effW * CanvasScale.X : effW;
            float drawH = AutoScale ? effH * CanvasScale.Y : effH;

            // === GLOW: рисуем перед основным спрайтом ===
            if (EnableGlow && GlowIntensity > 0.01f)
            {
                SB.DrawGlow(_textureId, drawX, drawY, drawW, drawH,
                            Color.R, Color.G, Color.B, Opacity,
                            GlowIntensity, projection, GlowSteps, GlowSpread);

               
            }

            // === Основной спрайт (оригинальная логика без изменений) ===
            if (AutoScale)
            {
                SB.Draw(_textureId,
                    drawX,
                    drawY,
                    drawW,
                    drawH,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Opacity,
                    projection);
            }
            else
            {
                SB.Draw(_textureId,
                    drawX,
                    drawY,
                    drawW,
                    drawH,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Opacity,
                    projection);
            }

            base.Draw(projection);
        }
    }
}