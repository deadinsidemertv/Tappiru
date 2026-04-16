using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class Slider : GameObject
    {
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

        public Slider(float min, float max, float x, float y, float width)
        {
            minValue = min;
            maxValue = max;
            Value = Math.Clamp(Value, min, max);

            int lineTexture = TextureManager.GetTexture("slider_line");

            // === Линия ===
            line = new SpriteObject(lineTexture, x, y, width, 8)
            {
                Color = new Color4(0.3f, 0.3f, 0.3f, 1f),
                Pivot = new Vector2(0.5f, 0.5f),
            };

            // === Ползунок ===
            point = new SpriteObject(lineTexture, x, y, 16, 40)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            // === Тексты (локальный ScaleMultiply) ===
            minValueText = new TextObject(min.ToString("F0"), x - width / 2f, y , 1f)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            maxValueText = new TextObject(max.ToString("F0"), x + width / 2f, y, 1f)
            {
                Color = Color4.White,
                ScaleMultiply = 0.25f,
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            ValueText = new TextObject(Value.ToString("F1"), x, y, 1f)
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
            var (pointLeft, pointTop, pointWidth, pointHeight) = point.GetDesignBounds();
            const float baseOffset = 5f;                    
            float extraOffset = 12f * ValueText.ScaleMultiply;

            float finalOffsetY = baseOffset + extraOffset;

            ValueText.Position = new Vector2(
                point.Position.X,                 
                pointTop - finalOffsetY              
            );

            ValueText.Text = Value.ToString("F1");
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

        public void SetValue(float newValue)
        {
            Value = Math.Clamp(newValue, minValue, maxValue);
            UpdatePointPositionFromValue();
        }
    }
}