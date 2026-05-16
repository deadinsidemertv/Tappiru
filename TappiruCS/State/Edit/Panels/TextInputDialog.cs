using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.Sprite;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit.Panels
{
    internal class TextInputModule : ModuleWindow
    {
        private readonly string _title;
        private readonly Action<string, string> _onOk;
        private readonly Action _onClose;

        private NineSliceSprite _panel = null!;
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

            _panel = new NineSliceSprite(TextureManager.GetTexture("module-window9"), 960, 540, 1100, 520)
            {
                SliceBorders = new Vector4(12, 12, 12, 12)
            };
            _panel.Layer = 15;

            // Заголовок
            var titleText = new TextObject(_title, 0, -200, 48f)
            {
                Color = Color4.White,
                Align = TextAlign.Center,
                FixedColor = true,
                Layer = 16
            };
            _panel.AddChild(titleText);
            // Верхнее поле
            var labelMain = new TextObject("Текст песни (как в игре):", 0, -100, 36f)
            {
                Color = new Color4(0.7f, 0.9f, 1f, 1f),
                Align = TextAlign.Center,
                FixedColor = true,
                Layer = 16
            };
            _panel.AddChild(labelMain);

            _inputMain = new InputField(0, -30, 900, 82)
            {
                PlaceHolderText = "Введите текст фразы...",
                Layer = 16
            };
            _inputMain.InputText.FontSize = 64f;
            _inputMain.PlaceHolder.FontSize = 48f;
            _panel.AddChild(_inputMain);
            // Нижнее поле
            var labelTrans = new TextObject("Транскрипция (romaji / lowercase):", 0, 30, 36f)
            {
                Color = new Color4(0.8f, 0.8f, 1f, 1f),
                Align = TextAlign.Center,
                FixedColor = true,
                Layer = 16
            };
            _panel.AddChild(labelTrans);

            _inputTrans = new InputField(0, 100, 900, 80)
            {
                PlaceHolderText = "Введите транскрипцию",
                Layer = 16
            };
            _inputTrans.InputText.FontSize = 64f;
            _inputTrans.PlaceHolder.FontSize = 48f;
            _panel.AddChild(_inputTrans);

            _okButton = new Button(0, 190, 300, 90, "SimpleGradientButton1", "OK")
            {
                ScaleMultiply = 1f,
                Layer = 16,
                
            };
            _okButton.Label.FontKey = "Game";
            _panel.AddChild(_okButton);
            _okButton.OnClick += OnOkClicked;

            obj.Add(_panel);

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