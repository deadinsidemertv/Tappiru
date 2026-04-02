using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class Background : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly int _textureId;
        private readonly Game _game;
        public float Opacity = 1f;
        public Background(SpriteBatch spriteBatch, int textureId,Game game)
        {
            _spriteBatch = spriteBatch;
            _textureId = textureId;
            _game = game;
        }

        public override void Draw(Matrix4 projection)
        {
            _spriteBatch.Draw(_textureId,
                 0, 0, _game.ClientSize.X, _game.ClientSize.Y,
                 0, 0, 1, 1,
                 1f, 1f, 1f, Opacity,
                 projection);

        }
    }
}
