using OpenTK.Mathematics;
using System.Reflection.Metadata.Ecma335;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
        public Color4 Color { get; set; } = Color4.White;
        public TextRender.TextAlign Align { get; set; } = TextRender.TextAlign.Center;

        public float ScaleMultiply { get; set; } = 1f;

        private readonly TextRender _textRender;

        public TextObject(TextRender textRender, string text, float x, float y, float scale = 1f)
        {
            _textRender = textRender;
            Text = text;
            Position = new Vector2(x, y);
            Scale = new Vector2(scale, scale);
        }

        public override void Draw(Matrix4 projection)
        {

            if (string.IsNullOrEmpty(Text)) return;

            if (Active)
            {
                // Применяем CanvasScale здесь (как было в твоей старой версии)
                _textRender.DrawString(
                    Text,
                    Position.X * CanvasScale.X,
                    Position.Y * CanvasScale.Y,
                    Scale.X * CanvasScale.X * ScaleMultiply,
                    Scale.Y * CanvasScale.Y * ScaleMultiply,
                    Color.R, Color.G, Color.B, Color.A,
                    projection,
                    Align
                );
            } 
            
        }
    }
}