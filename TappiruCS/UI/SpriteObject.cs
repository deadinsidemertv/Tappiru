// UI/SpriteObject.cs
using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;

namespace TappiruCS.UI
{
    public class SpriteObject : GameObject
    {
        public int _textureId;
        public Color4 Color { get; set; } = Color4.White;

        public SpriteObject(int textureId, float x, float y, float scaleX, float scaleY)
        {
            _textureId = textureId;
            Position = new Vector2(x, y);
            Scale = new Vector2(scaleX, scaleY);
        }

        public override void Draw(Matrix4 projection)
        {
            // Самая жёсткая защита именно здесь, потому что SpriteObject — самый частый
            if (!Active || Context == null || SB == null)
                return;

            var (dLeft, dTop, effW, effH) = GetDesignBounds();

            if (AutoScale)
            {
                SB.Draw(_textureId,
                    dLeft * CanvasScale.X,
                    dTop * CanvasScale.Y,
                    effW * CanvasScale.X,
                    effH * CanvasScale.Y,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Opacity,
                    projection);
            }
            else
            {
                SB.Draw(_textureId,
                    dLeft,
                    dTop,
                    effW,
                    effH,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Opacity,
                    projection);
            }
        }
    }
}