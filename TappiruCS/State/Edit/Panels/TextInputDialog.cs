using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit.Panels
{
    internal class TextInputModule : ModuleWindow
    {
        private readonly string _title;
        private readonly Action<string, string> _onOk;
        private readonly Action _onClose;

        private SpriteObject _panel = null!;
        private InputField _inputMain = null!;      // Основной текст
        private InputField _inputTrans = null!;     // Транскрипция
        private Button _okButton = null!;           // ← Добавили поле

        public TextInputModule(Scene scene, string title, Action<string, string> onOk, Action onClose)
            : base(scene)
        {
            _title = title;
            _onOk = onOk;
            _onClose = onClose;
        }

        public override void Show()
        {
            obj.Clear();

            _panel = new SpriteObject(TextureManager.GetTexture("module"), 960, 540, 1100, 520);

            // Заголовок
            var titleText = new TextObject(_title, 960, 380, 48f)
            {
                Color = Color4.White,
                Align = TextAlign.Center,
                FixedColor = true
            };

            // Верхнее поле
            var labelMain = new TextObject("Текст песни (как в игре):", 960, 460, 36f)
            {
                Color = new Color4(0.7f, 0.9f, 1f, 1f),
                Align = TextAlign.Center,
                FixedColor = true
            };

            _inputMain = new InputField(960, 520, 900, 80)
            {
                PlaceHolderText = "Введите текст фразы..."
            };

            // Нижнее поле
            var labelTrans = new TextObject("Транскрипция (romaji / lowercase):", 960, 620, 36f)
            {
                Color = new Color4(0.8f, 0.8f, 1f, 1f),
                Align = TextAlign.Center,
                FixedColor = true
            };

            _inputTrans = new InputField(960, 680, 900, 80)
            {
                PlaceHolderText = "konnichiwa sekai (оставьте пустым для автозаполнения)"
            };

            _okButton = new Button(960, 800, 300, 90, "button", "OK")
            {
                ScaleMultiply = 0.7f,
                Layer = 2
            };

            _okButton.OnClick += OnOkClicked;

            obj.Add(_panel);
            obj.Add(titleText);
            obj.Add(labelMain);
            obj.Add(_inputMain);
            obj.Add(labelTrans);
            obj.Add(_inputTrans);
            obj.Add(_okButton);

            base.Show();
        }

        private void OnOkClicked()
        {
            string mainText = _inputMain.Text.Trim();
            string transText = _inputTrans.Text.Trim();

            // Если транскрипция не заполнена — автозаполняем
            if (string.IsNullOrWhiteSpace(transText) && !string.IsNullOrWhiteSpace(mainText))
            {
                transText = CleanForTranscription(mainText);
            }

            _onOk?.Invoke(mainText, transText);
            Close();
        }

        private string CleanForTranscription(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return new string(text
                .ToLowerInvariant()
                .Where(c => char.IsLetter(c) || c == ' ')
                .ToArray())
                .Trim();
        }

        public override void Close()
        {
            base.Close();
            _onClose?.Invoke();
        }
    }
}