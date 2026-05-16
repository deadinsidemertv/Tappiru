using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.UI.Sprite;

namespace TappiruCS.State.Edit.Panels
{
    internal class ConfirmDialogModule : ModuleWindow
    {
        private Container _windowContainer;
        private readonly string _title;
        private readonly string _message;
        private readonly Action _onConfirm;

        public ConfirmDialogModule(Scene scene, string title, string message, Action onConfirm)
            : base(scene)
        {
            _title = title;
            _message = message;
            _onConfirm = onConfirm;
        }

        public override void Show()
        {
            

            // Основной контейнер окна
            _windowContainer = new Container(800, 420)
            {
                LocalPosition = new Vector2(960, 540), // центр экрана
                Pivot = new Vector2(0.5f, 0.5f)
            };

            // Фон окна
            var background = new NineSliceSprite(TextureManager.GetTexture("module-window8"), 0, 0, 800, 420)
            {
                Color = new Color4(0.5f, 0.5f, 0.5f, 1f),
                Layer = 15,
                AllowHover = false,
                SliceBorders = new Vector4(10,10,10,10)
            };

            // Заголовок
            var titleText = new TextObject(_title, 0, -140, 42f)
            {
                Color = Color4.White,
                Align = TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
                Layer = 16
            };

            // Сообщение
            var messageText = new TextObject(_message, 0, -40, 32f)
            {
                Color = new Color4(0.85f, 0.85f, 0.9f, 1f),
                Align = TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
                Layer = 16
            };

            // Кнопка "Да"
            var btnYes = new Button(-160, 80, 280, 90, "SimpleGradientButton1", "ДА, ВЫЙТИ")
            {
                Layer = 16,
                ScaleMultiply = 1f
            };
            btnYes.Label.Color = new Color4(0.9f, 0.3f, 0.3f, 1f);
            btnYes.Label.FontKey = "Game";
            btnYes.Label.ShadowOffset = new Vector2(-2, 1);
            btnYes.TextOffset = new Vector2(0, 10);
            btnYes.OnClick += () =>
            {
                _onConfirm?.Invoke();
                Close();
            };
            btnYes.Label.FontSize = 28f;

            // Кнопка "Нет"
            var btnNo = new Button(160, 80, 280, 90, "SimpleGradientButton1", "ОТМЕНА")
            {
                Layer = 16,
                ScaleMultiply = 1f
            };
            btnNo.Label.FontKey = "Game";
            btnNo.Label.ShadowOffset = new Vector2(-2, 1);
            btnNo.TextOffset = new Vector2(0, 10);
            btnNo.OnClick += Close;
            btnNo.Label.FontSize = 28f;

            _windowContainer.AddChild(background);
            _windowContainer.AddChild(titleText);
            _windowContainer.AddChild(messageText);
            _windowContainer.AddChild(btnYes);
            _windowContainer.AddChild(btnNo);
            _windowContainer.Layer = 20;


            obj.Add(_windowContainer);

            base.Show();
        }

        public override void Close()
        {
            if (_windowContainer != null)
            {
                _scene.Remove(_windowContainer);
                _windowContainer = null!;
            }

            base.Close();
        }
    }
}