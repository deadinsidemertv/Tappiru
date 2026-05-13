using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.Sprite;

namespace TappiruCS.UI
{
    public class ScrollContainer : GameObject
    {
        private readonly float _width;
        private readonly float _height;
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

        private readonly SpriteObject _debugArea;
        private readonly SpriteObject _hoverArea;

        public ClippingMask _clippingMask;

        public bool Clipping { get; set; } = true;

        public float HoverAreaX { get; set; }
        public float HoverAreaY { get; set; }
        public float HoverAreaWidth { get; set; }
        public float HoverAreaHeight { get; set; }

        public float Width => _width;
        public float Height => _height;

        public ScrollContainer(float x, float y, float width, float height, float itemSpacing = 80f)
        {
            LocalPosition = new Vector2(x, y);
            _width = width;
            _height = height;
            _itemSpacing = itemSpacing;

            _debugArea = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, _width, _height)
            {
                Color = new Color4(0.2f, 0.5f, 0.8f, 0.3f),
                Opacity = 0f,
                AllowHover = false,
                Layer = Layer - 1,
                Parent = this
            };
            AddChild(_debugArea);

            _hoverArea = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, _width, _height)
            {
                Color = new Color4(1f, 0.4f, 0.7f, 0.3f),
                Opacity = 0f,
                AllowHover = false,
                Layer = Layer - 1,
                Parent = this
            };
            AddChild(_hoverArea);

            HoverAreaX = 0;
            HoverAreaY = 300;
            HoverAreaWidth = _width;
            HoverAreaHeight = _height + 500;

            if (Debug)
            {
                _hoverArea.Opacity = 0.3f;
                _debugArea.Opacity = 0.3f;
            }



            UpdateHoverAreaVisual();
            UpdateClippingMask();
        }
        private void UpdateClippingMask()
        {
            // Удаляем старую маску, если она есть
            if (_clippingMask != null)
            {
                RemoveChild(_clippingMask);
                _clippingMask = null;
            }

            if (Clipping)
            {
                _clippingMask = new ClippingMask(85, 300, _width, _height);
                AddChild(_clippingMask);
            }
        }
        public void SetClipping(bool enable)
        {
            if (Clipping == enable) return;

            Clipping = enable;
            UpdateClippingMask();
        }
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

            if (item is Container container)
                container.RecalculateSize();

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
            foreach (var item in _items) RemoveChild(item);
            _items.Clear();
            _targetOffsetY = 0f;
            _currentOffsetY = 0f;
            RecalcMaxScroll();
            ApplyPositions();
        }

        public void Scroll(float deltaY)
        {
            _targetOffsetY -= deltaY * _scrollSpeed;
            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
        }

        public void ScrollToIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            float targetY = 0f;
            for (int i = 0; i < index; i++)
                targetY += GetItemHeight(_items[i]) + _itemSpacing;

            float itemCenter = targetY + GetItemHeight(_items[index]) / 2f;
            _targetOffsetY = Math.Clamp(itemCenter - _height / 2f, 0f, _maxScroll);
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
            RecalcMaxScroll();
            _currentOffsetY = MathHelper.Lerp(_currentOffsetY, _targetOffsetY, _smoothness * dt);
            ApplyPositions();
            HandleInput(mouse);
        }

        private void HandleInput(MouseState mouse)
        {
            float vx = mouse.X / CanvasScale.X;
            float vy = mouse.Y / CanvasScale.Y;

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

        // ==================== ИСПРАВЛЕННАЯ ЛОГИКА ====================
        private void ApplyPositions()
        {
            float currentY = -_currentOffsetY;   // начинаем от верхней видимой границы

            

            foreach (var item in _items)
            {
                float itemHeight = GetItemHeight(item);

                // Все GameObject позиционируются по ЦЕНТРУ
                float centerY = currentY + (itemHeight / 2f);

                item.LocalPosition = new Vector2(_horizontalPadding, centerY);

               

                // Переходим к следующему элементу
                currentY += itemHeight + _itemSpacing;
            }


        }

        private float GetItemHeight(GameObject item)
        {
            if (item is Container container)
            {
                container.RecalculateSize();
                float h = container.MaxHeight;

                if (h < 50f)
                {
                   
                    h = 250f;
                }
                return h;
            }

            // Для обычных SpriteObject и других GameObject
            return item.Scale.Y;
        }

        public void RecalcMaxScroll()
        {
            _contentHeight = 0f;

            foreach (var item in _items)
            {
                _contentHeight += GetItemHeight(item) + _itemSpacing;
            }

            if (_items.Count > 0)
                _contentHeight -= _itemSpacing;

            _maxScroll = Math.Max(0f, _contentHeight - _height / 2);

            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
            _currentOffsetY = Math.Clamp(_currentOffsetY, 0f, _maxScroll);

           
        }
    }
}