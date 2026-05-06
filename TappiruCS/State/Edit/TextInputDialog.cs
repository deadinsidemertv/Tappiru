using OpenTK.Mathematics;
using System;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    internal class TextInputModule : ModuleWindow
    {
        private readonly string _title;
        private readonly Action<string> _onOk;
        private readonly Action _onClose;

        private SpriteObject _panel = null!;
        private InputField _input = null!;
        private Button _okButton = null!;

        public TextInputModule(Scene scene, string title, Action<string> onOk, Action onClose)
            : base(scene)
        {
            _title = title;
            _onOk = onOk;
            _onClose = onClose;
        }

        public override void Show()
        {
            obj.Clear();

            _panel = new SpriteObject(TextureManager.GetTexture("module"), 960, 540, 1100, 400);

            _input = new InputField(960, 520, 900, 80)
            {
                PlaceHolderText = _title
            };

            _okButton = new Button(960, 650, 300, 90, "button", "OK")
            {
                ScaleMultiply = 0.7f,
                Layer = 2
            };

            _okButton.OnClick += () =>
            {
                _onOk?.Invoke(_input.Text);
                Close();
            };

            obj.Add(_panel);
            obj.Add(_input);
            obj.Add(_okButton);

            base.Show();
        }

        public override void Close()
        {
            base.Close();
            _onClose?.Invoke();
        }
    }
}