using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ScrollContainer : GameObject
    {
        private readonly float _width;
        private readonly float _height;
        private readonly float _itemHeight;
        private readonly float _itemSpacing;
        private readonly float _scrollSpeed = 120f;
        private readonly float _smoothness = 14f;
        private readonly float _horizontalPadding = 10f;

        private float _targetOffsetY = 0f;
        private float _currentOffsetY = 0f;
        private float _maxScroll = 0f;
        private float _contentHeight = 0f;

        private bool _isDragging = false;
        private float _lastMouseY = 0f;

        private readonly List<GameObject> _items = new();
        private readonly SpriteObject _debugArea;   // синяя область (только для визуализации)
        private readonly SpriteObject _hoverArea;    // розовая область (для хит-теста)

        // Настройки области ховера (локальные координаты относительно контейнера)
        public float HoverAreaX { get; set; }
        public float HoverAreaY { get; set; }
        public float HoverAreaWidth { get; set; }
        public float HoverAreaHeight { get; set; }

        public float Width => _width;
        public float Height => _height;

        public ScrollContainer(float x, float y, float width, float height, float itemHeight, float itemSpacing = 5f)
        {
            LocalPosition = new Vector2(x, y);
            _width = width;
            _height = height;
            _itemHeight = itemHeight;
            _itemSpacing = itemSpacing;

            // Синяя область (дебаг) – всегда отображается во весь контейнер
            _debugArea = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, _width, _height)
            {
                Color = new Color4(0.2f, 0.5f, 0.8f, 0.3f),
                Opacity = 0.3f,
                AllowHover = false,
                Layer = Layer - 1,
                Parent = this
            };
            AddChild(_debugArea);

            // Розовая область (ховер) – по умолчанию тоже весь контейнер, но можно изменить
            _hoverArea = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, _width, _height)
            {
                Color = new Color4(1f, 0.4f, 0.7f, 0.3f), // розовый
                Opacity = 0.3f,
                AllowHover = false,
                Layer = Layer - 1,
                Parent = this
            };
            AddChild(_hoverArea);

            // Инициализируем зону ховера значениями по умолчанию
            HoverAreaX = 0;
            HoverAreaY = 300;
            HoverAreaWidth = _width;
            HoverAreaHeight = _height+500;

            UpdateHoverAreaVisual();
        }

        // Обновление визуального представления розовой области (вызывается при изменении настроек)
        private void UpdateHoverAreaVisual()
        {
            _hoverArea.LocalPosition = new Vector2(HoverAreaX, HoverAreaY);
            _hoverArea.Scale = new Vector2(HoverAreaWidth, HoverAreaHeight);
        }

        public void SetHoverArea(float x, float y, float w, float h)
        {
            HoverAreaX = x;
            HoverAreaY = y;
            HoverAreaWidth = w;
            HoverAreaHeight = h;
            UpdateHoverAreaVisual();
        }

        public void AddItem(GameObject item)
        {
            _items.Add(item);
            AddChild(item);
            RecalcMaxScroll();
            ApplyPositions();
        }

        public void RemoveItem(GameObject item)
        {
            if (_items.Remove(item))
            {
                RemoveChild(item);
                RecalcMaxScroll();
                ApplyPositions();
            }
        }

        public void ClearItems()
        {
            foreach (var item in _items)
                RemoveChild(item);
            _items.Clear();
            _targetOffsetY = 0f;
            _currentOffsetY = 0f;
            RecalcMaxScroll();
            ApplyPositions();
        }

        public void ScrollToIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            float elementCenter = index * (_itemHeight + _itemSpacing) + _itemHeight / 2f;
            float viewCenter = _height / 2f;
            float target = elementCenter - viewCenter;
            _targetOffsetY = Math.Clamp(target, 0f, _maxScroll);
        }

        public void Scroll(float deltaY)
        {
            _targetOffsetY -= deltaY * _scrollSpeed;
            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
        }

        public void ResetScroll()
        {
            _targetOffsetY = 0f;
            _currentOffsetY = 0f;
            ApplyPositions();
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            float dt = (float)deltaTime;
            _currentOffsetY = MathHelper.Lerp(_currentOffsetY, _targetOffsetY, _smoothness * dt);
            ApplyPositions();
            HandleInput(mouse);
        }

        private void HandleInput(MouseState mouse)
        {
            float vx = mouse.X / CanvasScale.X;
            float vy = mouse.Y / CanvasScale.Y;

            // Проверяем попадание только в розовую область (ховер)
            bool over = _hoverArea.IsPointInside(vx, vy);

            float scrollDelta = mouse.ScrollDelta.Y;
            if (Math.Abs(scrollDelta) > 0.01f && over)
                Scroll(scrollDelta);

            if (mouse.IsButtonDown(MouseButton.Left) && over)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMouseY = mouse.Y;
                }
                else
                {
                    float delta = mouse.Y - _lastMouseY;
                    _targetOffsetY -= delta * 2f;
                    _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
                    _lastMouseY = mouse.Y;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private void ApplyPositions()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                float y = i * (_itemHeight + _itemSpacing) - _currentOffsetY;
                _items[i].LocalPosition = new Vector2(_horizontalPadding, y);
            }
        }

        public void RecalcMaxScroll()
        {
            _contentHeight = _items.Count * (_itemHeight + _itemSpacing);
            _maxScroll = Math.Max(0f, _contentHeight - _height / 2);
            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
            _currentOffsetY = Math.Clamp(_currentOffsetY, 0f, _maxScroll);
        }
    }
}