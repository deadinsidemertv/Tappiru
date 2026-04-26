using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.UI
{
    public class Slider : GameObject
    {
        public SpriteObject point;
        public SpriteObject line;

        public event Action<float> OnValueChanged;
        public float minValue { get; private set; } = 0f;
        public float maxValue { get; set; } = 100f;

        private float _value = 50f;
        public float Value
        {
            get => _value;
            private set
            {
                float clamped = Math.Clamp(value, minValue, maxValue);
                if (Math.Abs(clamped - _value) > 0.001f)
                {
                    _value = clamped;
                    OnValueChanged?.Invoke(_value);
                    UpdatePointPositionFromValue();
                }
            }
        }

        public bool _isDragging = false;

        public Slider(float min, float max, float x, float y, float width)
        {
            if (Debug)
            {
                var debugBg = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, width, 30)
                {
                    Opacity = 0.4f,
                    Color = Color4.Red,
                    AllowHover = false
                };
                AddChild(debugBg);
            }

            LocalPosition = new Vector2(x, y);
            minValue = min;
            maxValue = max;

            int lineTexture = TextureManager.GetTexture("slider_line");

            line = new SpriteObject(lineTexture, 0, 0, width, 4)
            {
                Color = Color4.Pink,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            point = new SpriteObject(TextureManager.GetTexture("sliderpoint"), 0, 0, 50, 50)
            {
                Color = Color4.Pink,
                Pivot = new Vector2(0.5f, 0.5f),
                AllowHover = true,
            };

            AddChild(line);
            AddChild(point);

            _value = Math.Clamp(50f, minValue, maxValue);
            UpdatePointPositionFromValue();
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            point.Description = $"{Math.Round(Value * 100)}%";
            UpdateDragging(mouse);
            UpdatePointPositionFromValue();
        }

        private void UpdateDragging(MouseState mouse)
        {
            float virtualMouseX = mouse.X / CanvasScale.X;
            float virtualMouseY = mouse.Y / CanvasScale.Y;

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
                var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();

                // Ограничиваем позицию мыши границами линии
                float clampedX = Math.Clamp(virtualMouseX, lineLeft, lineLeft + lineWidth);

                // Переводим в локальные координаты относительно слайдера
                float localX = clampedX - WorldPosition.X;
                point.LocalPosition = new Vector2(localX, point.LocalPosition.Y);

                UpdateValueFromPosition();
            }
        }

        private void UpdateValueFromPosition()
        {
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            // Мировая позиция точки = WorldPosition слайдера + локальная X точки
            float pointWorldX = WorldPosition.X + point.LocalPosition.X;
            float normalized = (pointWorldX - lineLeft) / lineWidth;
            Value = minValue + normalized * (maxValue - minValue);
        }

        private void UpdatePointPositionFromValue()
        {
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            float normalized = (_value - minValue) / (maxValue - minValue);
            float newWorldX = lineLeft + normalized * lineWidth;
            // Вычисляем локальную X относительно слайдера
            float localX = newWorldX - WorldPosition.X;
            // Вертикально позиционируем точку по Y линии (тоже локально)
            float localY = line.WorldPosition.Y - WorldPosition.Y;
            point.LocalPosition = new Vector2(localX, localY);
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