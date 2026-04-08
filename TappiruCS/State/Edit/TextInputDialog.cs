using OpenTK.Mathematics;
using System;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    internal class TextInputDialog
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly Scene _scene;
        private readonly string _title;
        private readonly Action<string> _onOk;
        private readonly Action _onClose;

        private SpriteObject _panel = null!;
        private InputField _input = null!;
        private Button _okButton = null!;

        public TextInputDialog(Game game, SpriteBatch spriteBatch, TextRender textRenderer, Scene scene,
                               string title, Action<string> onOk, Action onClose)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _scene = scene;
            _title = title;
            _onOk = onOk;
            _onClose = onClose;
        }

        public void Show()
        {
            _panel = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 1100, 400);

            _input = new InputField(_game, _spriteBatch, _textRenderer, 960, 520, 900, 80)
            {
                PlaceHolderText = _title
            };

            _okButton = new Button(_spriteBatch, _textRenderer, 960, 650, 300, 90, "button", "OK", Color4.White)
            {
                ScaleMultiply = 0.7f,
                Layer = 2
            };

            _okButton.OnClick += () =>
            {
                _onOk?.Invoke(_input.Text);
                Close();
            };

            _scene.Add(_panel);
            _scene.Add(_input);
            _scene.Add(_okButton);
        }

        private void Close()
        {
            _scene.Remove(_panel);
            _scene.Remove(_input);
            _scene.Remove(_okButton);
            _onClose?.Invoke();
        }
    }
}