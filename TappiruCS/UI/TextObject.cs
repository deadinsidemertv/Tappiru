using OpenTK.Mathematics;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
        public Color4 Color { get; set; } = Color4.White;
        public TextRender.TextAlign Align { get; set; } = TextRender.TextAlign.Center;

        private readonly TextRender _textRender;

        public TextObject(TextRender textRender, string text, float x, float y, float scale = 1f)
        {
            _textRender = textRender;
            Text = text;
            Position = new Vector2(x, y);
            Scale = new Vector2(scale, scale);

            // Для текста pivot по умолчанию — левый верхний угол (Align сам управляет выравниванием)
            Pivot = new Vector2(0.5f, 0.5f);
        }

        public override void Draw(Matrix4 projection)
        {
            if (string.IsNullOrEmpty(Text) || !Active) return;

            var (dLeft, dTop, effScaleX, effScaleY) = GetDesignBounds();

            _textRender.DrawString(
                Text,
                dLeft * CanvasScale.X,
                dTop * CanvasScale.Y,
                effScaleX * CanvasScale.X,
                effScaleY * CanvasScale.Y,
                Color.R, Color.G, Color.B, Color.A,
                projection,
                Align
            );
        }
    }
}