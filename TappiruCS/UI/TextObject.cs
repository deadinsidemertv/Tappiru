using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; }
        public float Spacing { get; set; } = 1.1f;
        public Color4 Color { get; set; } = Color4.White;
        public TextAlign Align { get; set; } = TextAlign.Center;

        private readonly TextRender _textRender;

        public TextObject(TextRender textRender, string text, float x, float y, float scale)
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
                Position.X*CanvasScale.X,
                Position.Y*CanvasScale.Y,
                Scale.X*CanvasScale.X,
                Scale.Y*CanvasScale.Y,
                Spacing,
                Color.R, Color.G, Color.B, Color.A,
                projection,
                Align
            );
            //Console.WriteLine("CanvasScale.X:" + CanvasScale.X + "   CanvasScale.Y: " + CanvasScale.Y);
        }
    }

}
