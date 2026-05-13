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

        // Единая зона: hover-область + clipping + debug-визуализация
        private readonly SpriteObject _zone;
        public ClippingMask _clippingMask;

        public float Width => _width;
        public float Height => _height;

        // Размер и позиция зоны в локальных координатах
        public float ZoneX => _zone.LocalPosition.X;
        public float ZoneY => _zone.LocalPosition.Y;
        public float ZoneWidth => _zone.Scale.X;
        public float ZoneHeight => _zone.Scale.Y;

        private bool _isPointerInsideZone = false;

        public ScrollContainer(float x, float y, float width, float height, float itemSpacing = 80f)
        {
            LocalPosition = new Vector2(x, y);
            _width = width;
            _height = height;
            _itemSpacing = itemSpacing;

            // Единый объект зоны — по нему считается всё
            _zone = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, _width, _height)
            {
                Color = new Color4(0.2f, 0.8f, 0.4f, 0.3f),
                Opacity = 0f,
                AllowHover = false,
                Layer = Layer + 1,
                Parent = this
            };
            AddChild(_zone);

            RebuildClippingMask();
        }

        public void SetZone(float x, float y, float w, float h)
        {
            _zone.LocalPosition = new Vector2(x, y);
            _zone.Scale = new Vector2(w, h);
            RebuildClippingMask();
        }

        private void RebuildClippingMask()
        {
            if (_clippingMask != null)
            {
                RemoveChild(_clippingMask);
                _clippingMask = null;
            }

            _clippingMask = new ClippingMask(
                _zone.LocalPosition.X,
                _zone.LocalPosition.Y,
                _zone.Scale.X,
                _zone.Scale.Y
            );
            AddChild(_clippingMask);
        }

        // =====================================================================
        // Items
        // =====================================================================

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

        // =====================================================================
        // Scroll
        // =====================================================================

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
            _targetOffsetY = Math.Clamp(itemCenter - ZoneHeight / 2f, 0f, _maxScroll);
        }

        public void ResetScroll()
        {
            _targetOffsetY = 0f;
            _currentOffsetY = 0f;
            ApplyPositions();
        }

        // =====================================================================
        // Update
        // =====================================================================

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            float dt = (float)deltaTime;

            // Debug-рамка — через базовый флаг Debug из GameObject
            _zone.Opacity = Debug ? 0.3f : 0f;

            RecalcMaxScroll();
            _currentOffsetY = MathHelper.Lerp(_currentOffsetY, _targetOffsetY, _smoothness * dt);
            ApplyPositions();

            HandleInput(mouse);
            UpdateItemsInteractivity();
        }

        // =====================================================================
        // Input
        // =====================================================================

        private void HandleInput(MouseState mouse)
        {
            float vx = mouse.X / CanvasScale.X;
            float vy = mouse.Y / CanvasScale.Y;

            _isPointerInsideZone = _zone.IsPointInside(vx, vy);

            float scrollDelta = mouse.ScrollDelta.Y;
            if (Math.Abs(scrollDelta) > 0.01f && _isPointerInsideZone)
                Scroll(scrollDelta);

            if (mouse.IsButtonDown(MouseButton.Left) && _isPointerInsideZone)
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

        private void UpdateItemsInteractivity()
        {
            foreach (var item in _items)
            {
                float worldCenterY = item.WorldPosition.Y;
                float halfH = GetItemHeight(item) / 2f;
                float itemTop = worldCenterY - halfH;
                float itemBottom = worldCenterY + halfH;

                float zoneWorldTop = WorldPosition.Y + ZoneY - ZoneHeight / 2f;
                float zoneWorldBottom = WorldPosition.Y + ZoneY + ZoneHeight / 2f;

                bool insideZone = itemBottom > zoneWorldTop && itemTop < zoneWorldBottom;

                item.AllowHover = insideZone && _isPointerInsideZone;
            }
        }

        // =====================================================================
        // Layout
        // =====================================================================

        private void ApplyPositions()
        {
            float currentY = -_currentOffsetY;

            foreach (var item in _items)
            {
                float itemHeight = GetItemHeight(item);
                item.LocalPosition = new Vector2(_horizontalPadding, currentY + itemHeight / 2f);
                currentY += itemHeight + _itemSpacing;
            }
        }

        private float GetItemHeight(GameObject item)
        {
            if (item is Container container)
            {
                container.RecalculateSize();
                float h = container.MaxHeight;
                return h < 50f ? 250f : h;
            }
            return item.Scale.Y;
        }

        public void RecalcMaxScroll()
        {
            _contentHeight = 0f;

            foreach (var item in _items)
                _contentHeight += GetItemHeight(item) + _itemSpacing;

            if (_items.Count > 0)
                _contentHeight -= _itemSpacing;

            _maxScroll = Math.Max(0f, _contentHeight - ZoneHeight);

            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, _maxScroll);
            _currentOffsetY = Math.Clamp(_currentOffsetY, 0f, _maxScroll);
        }
    }
}