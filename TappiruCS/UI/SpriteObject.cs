using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.UI
{
    public class SpriteObject : GameObject
    {
        public SpriteBatch _spriteBatch;
        public int _textureId;
        public float ScaleMultiply { get; set; } = 1.0f;
        public Color4 Color { get; set; } = Color4.White;


        public SpriteObject(SpriteBatch spriteBatch, int _textureID, float x, float y, float scaleX,float scaleY)
        {
            _spriteBatch = spriteBatch;
            Position = new Vector2(x, y);
            Scale = new Vector2(scaleX, scaleY);
            _textureId = _textureID;
        }


        public override void Draw(Matrix4 projection)
        {
            _spriteBatch.Draw(_textureId,
                 Position.X, Position.Y, Scale.X*ScaleMultiply, Scale.Y*ScaleMultiply,
                 0, 0, 1, 1,
                 Color.R, Color.G, Color.B, Color.A,
                 projection);

        }

    }
}
