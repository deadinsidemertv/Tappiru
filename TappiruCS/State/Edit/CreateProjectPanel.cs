using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    internal class CreateProjectModule : ModuleWindow
    {
        private readonly Action<string> _onProjectCreated;

        private SpriteObject _panel = null!;
        private InputField _titleInput = null!;
        private Button _mp3Button = null!;
        private Button _bgButton = null!;
        private Button _confirmButton = null!;

        private string? _mp3Path;
        private string? _bgPath;

        public CreateProjectModule(Scene scene, Action<string> onProjectCreated)
            : base(scene)
        {
            _onProjectCreated = onProjectCreated;
        }

        public override void Show()
        {
            // Очищаем на всякий случай
            obj.Clear();

            _panel = new SpriteObject(TextureManager.GetTexture("module"), 960, 540, 620, 820);

            _titleInput = new InputField(960, 320, 520, 70)
            {
                PlaceHolderText = "Название карты...",
                PlaceHolderColor = Color4.LightGray
            };

            _mp3Button = new Button(800, 480, 140, 140, "mp3", "MP3") { Layer = 2 };
            _bgButton = new Button(1120, 480, 140, 140, "png", "BG") { Layer = 2 };

            _mp3Button.OnClick += () => LoadFile("*.mp3", path => _mp3Path = path);
            _bgButton.OnClick += () => LoadFile("*.png;*.jpg", path => _bgPath = path);

            _confirmButton = new Button(960, 720, 500, 110, "button", "Создать карту")
            {
                ScaleMultiply = 0.65f,
                Layer = 2
            };
            _confirmButton.OnClick += ConfirmCreation;

            // Добавляем все объекты в список модуля
            obj.Add(_panel);
            obj.Add(_titleInput);
            obj.Add(_mp3Button);
            obj.Add(_bgButton);
            obj.Add(_confirmButton);

            base.Show(); // добавит все объекты в сцену
        }

        private void LoadFile(string filter, Action<string> onSuccess)
        {
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
                Close();                          // Закрываем модальное окно
                _onProjectCreated?.Invoke(tappPath);
            }
        }
    }
}