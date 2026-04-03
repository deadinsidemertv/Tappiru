using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State
{
    internal class EditState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();
        
        public InputField inputField;

        public EditState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }
        public void OnEnter()
        {
            

           
        }
        public void OnExit() { }
        public void Update(double currentTime) { }
        public void Render(Matrix4 projection) 
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }
    }
}
