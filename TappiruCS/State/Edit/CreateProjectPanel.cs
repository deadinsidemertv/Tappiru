// CreateProjectPanel.cs — модальное окно создания проекта
using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Модальное окно «Создать проект».
    /// Собирает: название карты, артист, путь к MP3, путь к фону.
    /// После подтверждения вызывает <see cref="ProjectCreator"/> и уведомляет
    /// владельца через колбэк <c>onProjectCreated(tappzPath)</c>.
    /// </summary>
    internal class CreateProjectModule : ModuleWindow
    {
        private readonly Action<string> _onProjectCreated;

        // ── UI-элементы ──────────────────────────────────────────────────────────
        private SpriteObject _panel = null!;
        private InputField _titleInput = null!;
        private InputField _artistInput = null!;
        private Button _mp3Button = null!;
        private Button _bgButton = null!;
        private Button _confirmButton = null!;

        // ── Данные формы ─────────────────────────────────────────────────────────
        private string? _mp3Path;
        private string? _bgPath;

        public CreateProjectModule(Scene scene, Action<string> onProjectCreated)
            : base(scene)
        {
            _onProjectCreated = onProjectCreated;
        }

        // ── Построение UI ────────────────────────────────────────────────────────
        public override void Show()
        {
            obj.Clear();

            _panel = new SpriteObject(TextureManager.GetTexture("module"), 960, 540, 620, 900);

            _titleInput = new InputField(960, 280, 520, 70)
            {
                PlaceHolderText = "Название карты...",
                PlaceHolderColor = Color4.LightGray
            };

            _artistInput = new InputField(960, 370, 520, 70)
            {
                PlaceHolderText = "Исполнитель...",
                PlaceHolderColor = Color4.LightGray
            };

            _mp3Button = new Button(800, 500, 140, 140, "mp3", "MP3") { Layer = 2 };
            _bgButton = new Button(1120, 500, 140, 140, "png", "BG") { Layer = 2 };

            _mp3Button.OnClick += () => PickFile("*.mp3", path => _mp3Path = path);
            _bgButton.OnClick += () => PickFile("*.png;*.jpg", path => _bgPath = path);

            _confirmButton = new Button(960, 740, 500, 110, "button", "Создать карту")
            {
                ScaleMultiply = 0.65f,
                Layer = 2
            };
            _confirmButton.OnClick += TryConfirm;

            obj.Add(_panel);
            obj.Add(_titleInput);
            obj.Add(_artistInput);
            obj.Add(_mp3Button);
            obj.Add(_bgButton);
            obj.Add(_confirmButton);

            base.Show();
        }

        // ── Логика ───────────────────────────────────────────────────────────────
        private static void PickFile(string filter, Action<string> onSuccess)
        {
            if (SharpFileDialog.NativeFileDialog.OpenDialog(null, "", out string? path)
                && path != null)
            {
                onSuccess(path);
            }
        }

        private void TryConfirm()
        {
            if (!IsFormComplete())
            {
                Console.WriteLine("[CreateProject] Заполните все поля: название, исполнитель, MP3 и фон.");
                return;
            }

            string? tappzPath = new ProjectCreator().Create(
                _titleInput.Text.Trim(),
                _artistInput.Text.Trim(),
                _mp3Path!,
                _bgPath!);

            if (!string.IsNullOrEmpty(tappzPath))
            {
                Close();
                _onProjectCreated(tappzPath);
            }
        }

        private bool IsFormComplete() =>
            !string.IsNullOrWhiteSpace(_titleInput.Text) &&
            !string.IsNullOrWhiteSpace(_artistInput.Text) &&
            _mp3Path != null &&
            _bgPath != null;
    }
}