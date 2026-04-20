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

        public event Action<float> OnValueChanged;
        public float minValue { get; private set; } = 0f;
        public float maxValue { get; set; } = 100f;

        private float _value = 50f;
        public float Value
        {
            get => _value;
            private set   // оставляем private set, чтобы снаружи только через SetValue
            {
                float clamped = Math.Clamp(value, minValue, maxValue);
                if (Math.Abs(clamped - _value) > 0.001f)   // чтобы не спамить при одинаковом значении
                {
                    _value = clamped;
                    OnValueChanged?.Invoke(_value);        // ← Вот главное!
                    UpdatePointPositionFromValue();
                    UpdateVisuals();
                }
            }
        }

        public List<SpriteObject> _markers = new List<SpriteObject>();
        public List<float> _markersTime = new List<float>();
        public List<string> _markersText = new List<string>();

        public bool _isDragging = false;

        public Slider(float min, float max, float x, float y, float width)
        {
            minValue = min;
            maxValue = max;

            // Сначала создаём все объекты
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

            // === Тексты ===
            minValueText = new TextObject(min.ToString("F0"), x - width / 2f, y, 1f)
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

            ValueText = new TextObject("", x, y, 1f)   // пока пусто
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

            // Теперь можно безопасно установить значение
            _value = Math.Clamp(50f, minValue, maxValue);   // напрямую в приватное поле
            UpdatePointPositionFromValue();
            UpdateVisuals();   // обновит ValueText
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
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            float normalized = (point.Position.X - lineLeft) / lineWidth;
            Value = minValue + normalized * (maxValue - minValue);   // через setter
        }

        private void UpdatePointPositionFromValue()
        {
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            float normalized = (_value - minValue) / (maxValue - minValue);
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

            ValueText.Text = Value.ToString("F2");
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

        public void SetValue(float newValue)
        {
            Value = newValue;
        }
    }
}