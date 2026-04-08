using OpenTK.Mathematics;
using System;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    internal class CreateProjectPanel
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly Scene _scene;
        private readonly Action<string> _onProjectCreated;

        private SpriteObject _panel = null!;
        private InputField _titleInput = null!;
        private Button _mp3Button = null!;
        private Button _bgButton = null!;
        private Button _confirmButton = null!;

        private string? _mp3Path;
        private string? _bgPath;

        public CreateProjectPanel(Game game, SpriteBatch spriteBatch, TextRender textRenderer,
                                  Scene scene, Action<string> onProjectCreated)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _scene = scene;
            _onProjectCreated = onProjectCreated;
        }

        public void Show()
        {
            _panel = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 620, 820);

            _titleInput = new InputField(_game, _spriteBatch, _textRenderer, 960, 320, 520, 70)
            {
                PlaceHolderText = "Название карты...",
                PlaceHolderColor = Color4.LightGray
            };

            _mp3Button = new Button(_spriteBatch, _textRenderer, 800, 480, 140, 140, "mp3", "MP3", Color4.White) { Layer = 2 };
            _bgButton = new Button(_spriteBatch, _textRenderer, 1120, 480, 140, 140, "png", "BG", Color4.White) { Layer = 2 };

            _mp3Button.OnClick += () => LoadFile("*.mp3", path => _mp3Path = path);
            _bgButton.OnClick += () => LoadFile("*.png;*.jpg", path => _bgPath = path);

            _confirmButton = new Button(_spriteBatch, _textRenderer, 960, 720, 500, 110, "button", "Создать карту", Color4.White)
            {
                ScaleMultiply = 0.65f,
                TextScale = 0.8f,
                Layer = 2
                
            };
            _confirmButton.OnClick += ConfirmCreation;

            _scene.Add(_panel);
            _scene.Add(_titleInput);
            _scene.Add(_mp3Button);
            _scene.Add(_bgButton);
            _scene.Add(_confirmButton);
        }

        private void LoadFile(string filter, Action<string> onSuccess)
        {
            // Можно улучшить фильтры, но для простоты оставляем
            if (SharpFileDialog.NativeFileDialog.OpenDialog(null, "", out string? path) && path != null)
                onSuccess(path);
        }

        private void ConfirmCreation()
        {
            if (string.IsNullOrWhiteSpace(_titleInput.Text) || _mp3Path == null || _bgPath == null)
            {
                Console.WriteLine("Не все поля заполнены! Укажите название, MP3 и фон.");
                return;
            }

            var creator = new ProjectCreator();
            string? tappPath = creator.Create(_titleInput.Text, _mp3Path, _bgPath);

            if (!string.IsNullOrEmpty(tappPath))
            {
                RemoveAll();
                _onProjectCreated?.Invoke(tappPath);
            }
        }

        private void RemoveAll()
        {
            _scene.Remove(_panel);
            _scene.Remove(_titleInput);
            _scene.Remove(_mp3Button);
            _scene.Remove(_bgButton);
            _scene.Remove(_confirmButton);
        }


    }
}