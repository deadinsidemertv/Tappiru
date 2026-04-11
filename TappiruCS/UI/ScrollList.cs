using Gtk;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ScrollList : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;

        public float ScaleMultiplyList = 0.7f;
        public float ScrollSpeed = 140f;
        public float Smoothness = 16f;           // плавность

        private float _targetScrollOffsetY = 0f;
        public float ScrollOffsetY { get; private set; } = 0f;

        public List<ListElementButton> Buttons { get; } = new();

        private float _itemHeight = 100f;        
        private float _itemSpacing = 10f;
        private float _visibleHeight;

        // Dragging
        private bool _isDragging = false;
        private float _lastMouseY = 0f;

        private int _hoveredIndex = -1;
        public int HoveredIndex => _hoveredIndex;

        public ScrollList(SpriteBatch spriteBatch, TextRender textRenderer,
                          float x, float y, float width, float height)
        {
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            Position = new Vector2(x, y);
            _visibleHeight = 400;
        }

        public void AddButton(ListElementButton button)
        {
            
            button.ScaleMultiply = ScaleMultiplyList;
            Buttons.Add(button);
            AddChild(button);
        }
        public void NotifyHoverChanged(ListElementButton button, bool isHovered)
        {
            int index = Buttons.IndexOf(button);
        }
        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            float dt = (float)deltaTime;

            ScrollOffsetY = MathHelper.Lerp(ScrollOffsetY, _targetScrollOffsetY, Smoothness * dt);

            float baseY = Position.Y;

            for (int i = 0; i < Buttons.Count; i++)
            {
                var btn = Buttons[i];

                btn.ScaleMultiply = ScaleMultiplyList;        

                float targetY = baseY + i * (_itemHeight + _itemSpacing) - ScrollOffsetY;


                float itemCenterY = targetY + _itemHeight / 2f;
                float viewCenterY = Position.Y + _visibleHeight / 2f;
                float distanceFromCenter = itemCenterY - viewCenterY;
                float normalizedDist = distanceFromCenter / (_visibleHeight * 0.5f); // 0.5f даёт ±1 точно на краях видимой области

                float maxOffset = 15f;
                float xOffset = maxOffset * normalizedDist * normalizedDist;


                btn.Position = new Vector2(Position.X+xOffset, targetY);
                btn.Active = IsButtonVisible(i);

            }

            HandleDragging(mouse);                                    
        }

        private void HandleDragging(MouseState mouse)
        {
            bool mouseOverList = IsPointInsideList(mouse.X, mouse.Y);

            if (mouse.IsButtonDown(MouseButton.Left) && mouseOverList)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMouseY = mouse.Y;
                }
                else
                {
                    float delta = mouse.Y - _lastMouseY;
                    _targetScrollOffsetY -= delta * 2.3f;     // чувствительность драга
                    _lastMouseY = mouse.Y;
                    ClampScroll();
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private bool IsPointInsideList(float x, float y)
        {
            float left = Position.X - 700f * EffectiveScaleMultiply;   // половина ширины 1400
            float right = left + 1400f * EffectiveScaleMultiply;
            float top = Position.Y;
            float bottom = top + _visibleHeight;

            return x >= left && x <= right && y >= top && y <= bottom;
        }

        public void Scroll(float deltaY) // от колёсика
        {
            _targetScrollOffsetY -= deltaY * ScrollSpeed;
            ClampScroll();
        }

        private void ClampScroll()
        {
            float contentHeight = Buttons.Count * (_itemHeight + _itemSpacing);
            float maxScroll = Math.Max(0, contentHeight - _visibleHeight);
            _targetScrollOffsetY = Math.Clamp(_targetScrollOffsetY, 0, maxScroll);
        }

        private bool IsButtonVisible(int index)
        {
            float itemTop = index * (_itemHeight + _itemSpacing) - ScrollOffsetY;
            return itemTop < _visibleHeight + 700 && itemTop + _itemHeight > -700;
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

        public void ResetScroll() => _targetScrollOffsetY = ScrollOffsetY = 0f;
    }
}