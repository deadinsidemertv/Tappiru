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
            private set   // оставляем private set, чтобы снаружи только через SetValue
            {
                float clamped = Math.Clamp(value, minValue, maxValue);
                if (Math.Abs(clamped - _value) > 0.001f)   // чтобы не спамить при одинаковом значении
                {
                    _value = clamped;
                    OnValueChanged?.Invoke(_value);        
                    UpdatePointPositionFromValue();
                    
                }
            }
        }

        public List<SpriteObject> _markers = new List<SpriteObject>();
        public List<float> _markersTime = new List<float>();
        public List<string> _markersText = new List<string>();

        public bool _isDragging = false;

        public Slider(float min, float max, float x, float y, float width)
        {

            if (Debug) 
            {
                var debugBg = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, width, 30) 
                {
                    Opacity = 0.4f,
                    Color = Color4.Red,
                    AllowHover =false
                };
                AddChild(debugBg);
            }
                

            LocalPosition = new Vector2(x, y);
            Description = Value+"%";
            minValue = min;
            maxValue = max;

            // Сначала создаём все объекты
            int lineTexture = TextureManager.GetTexture("slider_line");

            // === Линия ===
            line = new SpriteObject(lineTexture, 0, 0, width, 4)
            {
                Color = Color4.Pink,
                Pivot = new Vector2(0.5f, 0.5f),
            };

            // === Ползунок ===
            point = new SpriteObject(TextureManager.GetTexture("sliderpoint"), 0, 0, 50, 50)
            {
                Color = Color4.Pink,
                Pivot = new Vector2(0.5f, 0.5f),
                AllowHover = true,
            };

            

            AddChild(line);
            AddChild(point);


            // Теперь можно безопасно установить значение
            _value = Math.Clamp(50f, minValue, maxValue);   // напрямую в приватное поле
            UpdatePointPositionFromValue();
              // обновит ValueText
        }
        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime,mouse);
            
            point.Description = $"{Math.Round(Value*100)}%";

            UpdateDragging(mouse);
            UpdatePointPositionFromValue();// ← новое
            UpdateTextPositions();
           
        }
        private void UpdateTextPositions()
        {
            var (lineLeft, lineTop, lineWidth, lineHeight) = line.GetDesignBounds();

            float offsetBelow = -50f * this.ScaleMultiply;   // отступ для min/max вниз от линии

            
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

                point.WorldPosition = new Vector2(clampedX, point.WorldPosition.Y);

                UpdateValueFromPosition();
            }
        }

        private void UpdateValueFromPosition()
        {
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            float normalized = (point.WorldPosition.X - lineLeft) / lineWidth;
            Value = minValue + normalized * (maxValue - minValue);   // через setter
        }

        private void UpdatePointPositionFromValue()
        {
            if (line == null) return;

            var (lineLeft, _, lineWidth, _) = line.GetDesignBounds();
            float normalized = (_value - minValue) / (maxValue - minValue);
            float newX = lineLeft + normalized * lineWidth;

            point.WorldPosition = new Vector2(newX, line.WorldPosition.Y);
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