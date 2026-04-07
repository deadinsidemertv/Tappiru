using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class Slider : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRender;

        public SpriteObject point;
        public SpriteObject line;

        public TextObject minValueText;
        public TextObject maxValueText;
        public TextObject ValueText;

        public float minValue { get; private set; } = 0f;
        public float maxValue { get; private set; } = 100f;
        public float Value { get; private set; } = 50f;

        private bool _isDragging = false;

        public Slider(SpriteBatch spritebatch, TextRender textrender,
                      float min, float max, float x, float y, float width)
        {
            _spriteBatch = spritebatch;
            _textRender = textrender;

            minValue = min;
            maxValue = max;
            Value = Math.Clamp(Value, min, max);

            int lineTexture = TextureManager.GetTexture("slider_line");

            // Линия (фон слайдера)
            line = new SpriteObject(_spriteBatch, lineTexture, x, y, width, 8)
            {
                Color = new Color4(0.3f, 0.3f, 0.3f, 1f),
                Pivot = new Vector2(0.5f, 0.5f)
            };

            // Ползунок
            point = new SpriteObject(_spriteBatch, lineTexture, x, y, 16, 40)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            // Тексты
            minValueText = new TextObject(_textRender, min.ToString("F0"), x - width * 0.48f, y - 55)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            maxValueText = new TextObject(_textRender, max.ToString("F0"), x + width * 0.48f, y - 55)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            ValueText = new TextObject(_textRender, Value.ToString("F1"), x, y - 90)
            {
                Color = Color4.White,
                ScaleMultiply = 0.3f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            UpdatePointPositionFromValue();
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime);

            // Передаём CanvasScale всем детям
            line.CanvasScale = CanvasScale;
            point.CanvasScale = CanvasScale;
            minValueText.CanvasScale = CanvasScale;
            maxValueText.CanvasScale = CanvasScale;
            ValueText.CanvasScale = CanvasScale;

            UpdateDragging(mouse);
            UpdateVisuals();
        }

        private void UpdateDragging(MouseState mouse)
        {
            // === КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ ===
            // Преобразуем экранные координаты мыши в дизайн-координаты
            float virtualMouseX = mouse.X / CanvasScale.X;
            float virtualMouseY = mouse.Y / CanvasScale.Y;

            // Проверяем нажатие на ползунок
            if (point.IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                _isDragging = true;

            if (mouse.IsButtonReleased(MouseButton.Left))
                _isDragging = false;

            if (_isDragging)
            {
                // Получаем границы линии в дизайн-координатах
                var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();

                // Ограничиваем виртуальную позицию мыши границами линии
                float clampedX = Math.Clamp(virtualMouseX, lineLeft, lineLeft + lineWidth);

                point.Position = new Vector2(clampedX, point.Position.Y);

                UpdateValueFromPosition();
            }
        }

        private void UpdateValueFromPosition()
        {
            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();

            float normalized = (point.Position.X - lineLeft) / lineWidth;
            Value = minValue + normalized * (maxValue - minValue);
            Value = Math.Clamp(Value, minValue, maxValue);
        }

        private void UpdatePointPositionFromValue()
        {
            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();

            float normalized = (Value - minValue) / (maxValue - minValue);
            float newX = lineLeft + normalized * lineWidth;

            point.Position = new Vector2(newX, line.Position.Y);
        }

        private void UpdateVisuals()
        {
            ValueText.Position = new Vector2(point.Position.X, point.Position.Y - 80);
            ValueText.Text = Value.ToString("F1");
        }

        public override void Draw(Matrix4 projection)
        {
            line.Draw(projection);
            point.Draw(projection);
            minValueText.Draw(projection);
            maxValueText.Draw(projection);
            ValueText.Draw(projection);
        }

        public void SetValue(float newValue)
        {
            Value = Math.Clamp(newValue, minValue, maxValue);
            UpdatePointPositionFromValue();
        }
    }
}