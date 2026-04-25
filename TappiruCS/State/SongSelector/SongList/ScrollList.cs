using Gtk;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Pango;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.State.SongSelector.SongList
{
    public class ScrollList : GameObject
    {
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

        public bool mouseOverList = false;

        // === ПЛАВНЫЕ СМЕЩЕНИЯ ДЛЯ КАЖДОЙ КНОПКИ ===
        private float[] _hoverX;
        private float[] _hoverY;

        public ListElementButton SelectedButton { get; private set; } = null;
        public int SelectedIndex { get; private set; } = -1;

        public event Action<ListElementButton> OnSelectionChanged;   // опционально, если нужно уведомлять
        public ScrollList(float x, float y, float width, float height)
        {
            Description = string.Empty;
            LocalPosition = new Vector2(x, y);
            _visibleHeight = 400;

            _hoverX = new float[0];
            _hoverY = new float[0];
        }
        public void SelectButton(int index)
        {
            if (index < 0 || index >= Buttons.Count)
            {
                DeselectAll();
                return;
            }

            // Снимаем выделение со всех
            foreach (var btn in Buttons)
                btn.SetSelected(false);

            // Выделяем нужную
            var selected = Buttons[index];
            selected.SetSelected(true);

            SelectedButton = selected;
            SelectedIndex = index;

            ScrollToIndex(index);

            OnSelectionChanged?.Invoke(selected);


        }
        public void AddButton(ListElementButton button)
        {

            button.ScaleMultiply = ScaleMultiplyList;
            Buttons.Add(button);
            AddChild(button);

            // Увеличиваем массивы
            Array.Resize(ref _hoverX, Buttons.Count);
            Array.Resize(ref _hoverY, Buttons.Count);

            button.OnClick += () => SelectButton(Buttons.IndexOf(button));
        }
        public void NotifyHoverChanged(ListElementButton button, bool isHovered)
        {
            int index = Buttons.IndexOf(button);
            if (index == -1) return;
            if (isHovered)
            {
                _hoveredIndex = index;
            }
            else if (_hoveredIndex == index)
            {
                _hoveredIndex = -1;        // курсор ушёл с этой кнопки
            }
        }
        public override void Update(double deltaTime, MouseState mouse)
        {
            //base.Update(deltaTime, mouse);
            mouseOverList = IsPointInsideList(mouse.X, mouse.Y);
            float dt = (float)deltaTime;

            ScrollOffsetY = MathHelper.Lerp(ScrollOffsetY, _targetScrollOffsetY, Smoothness * dt);
            for (int i = 0; i < Buttons.Count; i++)
            {
                float targetX = 0f;
                float targetY = 0f;

                if (_hoveredIndex != -1)
                {
                    if (i == _hoveredIndex)
                        targetX = -100f;           // главная кнопка — влево
                    else if (i < _hoveredIndex)
                        targetY = -25f;            // кнопки выше — вверх
                    else
                        targetY = 25f;             // кнопки ниже — вниз
                }

                // Плавное движение
                _hoverX[i] = MathHelper.Lerp(_hoverX[i], targetX, 6f * dt);
                _hoverY[i] = MathHelper.Lerp(_hoverY[i], targetY, 6f * dt);
            }
            // Обновляем позиции кнопок
            for (int i = 0; i < Buttons.Count; i++)
            {

                var btn = Buttons[i];
                btn.ScaleMultiply = ScaleMultiplyList;

                float targetY = i * (_itemHeight + _itemSpacing) - ScrollOffsetY;
                // парабола и hover-смещения
                float itemCenterY = targetY + _itemHeight / 2f;
                float viewCenterY = _visibleHeight / 2f;
                float distanceFromCenter = itemCenterY - viewCenterY;
                float normalizedDist = distanceFromCenter / (_visibleHeight * 0.5f);
                float parabolaX = 15f * normalizedDist * normalizedDist;

                float finalX = parabolaX + _hoverX[i];
                float finalY = targetY + _hoverY[i];

                btn.LocalPosition = new Vector2(finalX, finalY);
                btn.Active = IsButtonVisible(i);
            }

            HandleDragging(mouse);

            base.Update(deltaTime, mouse);
        }

        private void HandleDragging(MouseState mouse)
        {
            

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
            float left = WorldPosition.X - 900f * EffectiveScaleMultiply;   // половина ширины 1400
            float right = left + 1400f * EffectiveScaleMultiply;
            float top = 0;
            float bottom = 1080;

            return x >= left && x <= right && y >= top && y <= bottom;
        }

        public void Scroll(float deltaY) // от колёсика
        {
            if (!mouseOverList)
                return;

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
        private void DeselectAll()
        {
            foreach (var btn in Buttons)
                btn.SetSelected(false);

            SelectedButton = null;
            SelectedIndex = -1;
        }
        public void ResetScroll() => _targetScrollOffsetY = ScrollOffsetY = 0f;

        public void ScrollToIndex(int index)
        {
            if (index < 0 || index >= Buttons.Count) return;

            float contentY = index * (_itemHeight + _itemSpacing);
            float buttonCenter = contentY + _itemHeight / 2f;
            float targetOffset = buttonCenter - _visibleHeight / 2f;

            // Ограничиваем допустимыми пределами прокрутки
            float maxScroll = Math.Max(0, Buttons.Count * (_itemHeight + _itemSpacing) - _visibleHeight);
            targetOffset = Math.Clamp(targetOffset, 0, maxScroll);

            _targetScrollOffsetY = targetOffset;
        }
    }
}