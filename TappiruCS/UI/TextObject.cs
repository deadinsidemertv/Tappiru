using OpenTK.Mathematics;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
        public Color4 Color { get; set; } = Color4.White;
        public TextRender.TextAlign Align { get; set; } = TextRender.TextAlign.Center;

        public float ScaleMultiply { get; set; } = 1.0f;

        private readonly TextRender _textRender;

        public TextObject(TextRender textRender, string text, float x, float y, float scale = 1.0f)
        {
            _textRender = textRender;
            Text = text;
            Position = new Vector2(x, y);
            Scale = new Vector2(scale, scale);
        }

        public override void Draw(Matrix4 projection)
        {
            if (string.IsNullOrEmpty(Text)) return;

            _textRender.DrawString(
                Text,
                Position.X,
                Position.Y,
                Scale.X * ScaleMultiply,
                Scale.Y * ScaleMultiply,
                Color.R, Color.G, Color.B, Color.A,
                projection,
                Align
            );
        }

        // Удобная перегрузка для быстрого создания
        public TextObject(TextRender textRender, string text, Vector2 position, float scale = 1.0f)
            : this(textRender, text, position.X, position.Y, scale)
        { }
    }
}