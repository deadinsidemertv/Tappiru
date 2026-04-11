using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
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
        public float maxValue { get; set; } = 100f;
        public float Value { get; private set; } = 50f;

        public List<SpriteObject> _markers = new List<SpriteObject>();
        public List<float> _markersTime = new List<float>();
        public List<string> _markersText = new List<string>();

        public bool _isDragging = false;

        public Slider(SpriteBatch spritebatch, TextRender textrender,
                      float min, float max, float x, float y, float width)
        {
            _spriteBatch = spritebatch;
            _textRender = textrender;

            minValue = min;
            maxValue = max;
            Value = Math.Clamp(Value, min, max);

            int lineTexture = TextureManager.GetTexture("slider_line");

            // === Линия ===
            line = new SpriteObject(_spriteBatch, lineTexture, x, y, width, 8)
            {
                Color = new Color4(0.3f, 0.3f, 0.3f, 1f),
                Pivot = new Vector2(0.5f, 0.5f),
            };

            // === Ползунок ===
            point = new SpriteObject(_spriteBatch, lineTexture, x, y, 16, 40)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            // === Тексты (локальный ScaleMultiply) ===
            minValueText = new TextObject(_textRender, min.ToString("F0"), x - width / 2f, y , 1f)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            maxValueText = new TextObject(_textRender, max.ToString("F0"), x + width / 2f, y, 1f)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            ValueText = new TextObject(_textRender, Value.ToString("F1"), x, y, 1f)
            {
                Color = Color4.White,
                ScaleMultiply = 0.2f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            AddChild(line);
            AddChild(point);
            AddChild(minValueText);
            AddChild(maxValueText);
            AddChild(ValueText);
            UpdatePointPositionFromValue();
        }

        public float GetPositionFromTime(float timeSeconds)
        {
            float t = (timeSeconds - minValue) / (maxValue - minValue);
            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            return lineLeft + t * lineWidth;
        }
        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime);

            UpdateDragging(mouse);
            UpdatePointPositionFromValue();// ← новое
            UpdateTextPositions();
            UpdateVisuals();
        }
        private void UpdateTextPositions()
        {
            var (lineLeft, lineTop, lineWidth, lineHeight) = line.GetDesignBounds();

            float offsetBelow = -50f * this.ScaleMultiply;   // отступ для min/max вниз от линии

            minValueText.Position = new Vector2(lineLeft, lineTop + lineHeight / 2 + offsetBelow);
            maxValueText.Position = new Vector2(lineLeft + lineWidth, lineTop + lineHeight / 2 + offsetBelow);
        }
        private void UpdateDragging(MouseState mouse)
        {
            // Преобразуем экранные координаты мыши в дизайн-координаты (как в Scene)
            float virtualMouseX = mouse.X / CanvasScale.X;
            float virtualMouseY = mouse.Y / CanvasScale.Y;

            // === УЛУЧШЕННАЯ ПРОВЕРКА НАЖАТИЯ ===
            // Проверяем нажатие НЕ только на point, а на любую часть слайдера (line или point)
            bool clickedOnSlider = line.IsPointInside(virtualMouseX, virtualMouseY) ||
                                   point.IsPointInside(virtualMouseX, virtualMouseY);

            if (clickedOnSlider && mouse.IsButtonPressed(MouseButton.Left))
            {
                _isDragging = true;
            }

            if (mouse.IsButtonReleased(MouseButton.Left))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                // Получаем актуальные границы линии с учётом всех ScaleMultiply родителей
                var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();

                // Ограничиваем позицию мыши границами линии
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
            // Получаем актуальные границы ползунка (с учётом всех масштабов)
            var (pointLeft, pointTop, pointWidth, pointHeight) = point.GetDesignBounds();

            // Фиксированный отступ от верхней границы ползунка вверх
            // Это значение почти не зависит от размера текста
            const float baseOffset = 5f;                    // основное расстояние от ползунка до текста
            float extraOffset = 12f * ValueText.ScaleMultiply; // небольшой дополнительный отступ, зависящий от размера текста

            float finalOffsetY = baseOffset + extraOffset;

            ValueText.Position = new Vector2(
                point.Position.X,                    // точно по центру ползунка по X
                pointTop - finalOffsetY              // выше верхней границы ползунка
            );

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