using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core;
using TappiruCS.Render;
using OpenTK.Mathematics;

namespace TappiruCS.UI
{
    public class UIElement : GameObject
    {
        public SpriteBatch _spriteBatch;
        public int _textureId;

        public SpriteObject(SpriteBatch spriteBatch, int textureID, float x, float y, float scaleX, float scaleY)
        {
            _spriteBatch = spriteBatch;
            _textureId = textureID;
            Position = new Vector2(x, y);
            Scale = new Vector2(scaleX, scaleY);
        }
    }
}
