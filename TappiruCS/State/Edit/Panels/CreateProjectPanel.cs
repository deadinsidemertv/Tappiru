// CreateProjectPanel.cs — модальное окно создания проекта
using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.State.Edit.SaveLoad;
using TappiruCS.UI;

namespace TappiruCS.State.Edit.Panels
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

            // Фоновая панель (можно заменить на более красивую текстуру, если есть)
            _panel = new SpriteObject(TextureManager.GetTexture("module"), 960, 540, 620, 900);

            // Поле названия карты (опционально)
            _titleInput = new InputField(960, 280, 520, 70)
            {
                PlaceHolderText = "Название карты (опционально)...",
                PlaceHolderColor = Color4.LightGray
            };

            // Поле исполнителя (опционально)
            _artistInput = new InputField(960, 370, 520, 70)
            {
                PlaceHolderText = "Исполнитель (опционально)...",
                PlaceHolderColor = Color4.LightGray
            };

            // Кнопки выбора MP3 и фона (обязательные поля)
            _mp3Button = new Button(800, 500, 140, 140, "mp3", "MP3") { Layer = 2 };
            _bgButton = new Button(1120, 500, 140, 140, "png", "BG") { Layer = 2 };

            _mp3Button.OnClick += () => PickFile("*.mp3", path => _mp3Path = path);
            _bgButton.OnClick += () => PickFile("*.png;*.jpg", path => _bgPath = path);

            // Кнопка подтверждения с красивой текстурой "blue_panel"
            _confirmButton = new Button(960, 740, 500, 110, "blue_panel", "Создать карту")
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
            // SharpFileDialog - ваш диалог выбора файлов
            if (SharpFileDialog.NativeFileDialog.OpenDialog(null, "", out string? path)
                && path != null)
            {
                onSuccess(path);
            }
        }

        private void TryConfirm()
        {
            // Проверяем, что MP3 и фон выбраны (обязательные поля)
            if (string.IsNullOrEmpty(_mp3Path) || string.IsNullOrEmpty(_bgPath))
            {
                Console.WriteLine("[CreateProject] Не выбраны MP3 или фоновое изображение.");
                return;
            }

            // Название и исполнитель могут быть пустыми – позже зададутся в настройках
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

        // Старая проверка больше не нужна, замена на более мягкую (только MP3 и BG обязательны)
        // private bool IsFormComplete() => ...
    }
}