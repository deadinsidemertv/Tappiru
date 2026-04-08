using OpenTK.Mathematics;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class SpriteObject : GameObject
    {
        public SpriteBatch _spriteBatch;
        public int _textureId;
        public Color4 Color { get; set; } = Color4.White;

        public SpriteObject(SpriteBatch spriteBatch, int _textureID, float x, float y, float scaleX, float scaleY)
        {
            _spriteBatch = spriteBatch;
            Position = new Vector2(x, y);
            Scale = new Vector2(scaleX, scaleY);
            _textureId = _textureID;
        }

        public override void Draw(Matrix4 projection)
        {
            if (!Active) return;

            var (dLeft, dTop, effW, effH) = GetDesignBounds();

            if (AutoScale)
            {
                _spriteBatch.Draw(_textureId,
                    dLeft * CanvasScale.X,
                    dTop * CanvasScale.Y,
                    effW * CanvasScale.X,
                    effH * CanvasScale.Y,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Color.A,
                    projection);
            }
            else
            {
                _spriteBatch.Draw(_textureId,
                    dLeft,
                    dTop,
                    effW,
                    effH,
                    0, 0, 1, 1,
                    Color.R, Color.G, Color.B, Color.A,
                    projection);
            }
        }
    }
}