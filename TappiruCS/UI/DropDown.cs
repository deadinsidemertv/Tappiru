using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Tween;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.UI
{
    public class DropDown : GameObject
    {
        // === Данные ===
        private List<string> _items = new List<string>();
        private int _selectedIndex = -1;

        // === Компоненты ===
        private Button _mainButton;
        private SpriteObject _listBackground;
        private List<Button> _itemButtons = new List<Button>();
        private bool _isExpanded = false;

        // === Настройки ===
        public string PlaceholderText { get; set; } = "Select...";
        public float FontSize { get; set; } = 24f;
        public Color4 TextColor { get; set; } = Color4.White;
        public TextAlign TextAlign { get; set; } = TextAlign.Left;
        public Vector2 ItemPadding { get; set; } = new Vector2(10, 5);
        public float ItemHeight { get; set; } = 0f; // 0 = auto
        public float MaxDropHeight { get; set; } = 300f;

        // Стилизация
        public string MainButtonTexture { get; set; } = "button_bg";
        public string ListBackgroundTexture { get; set; } = "panel_bg";
        public Color4 NormalColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public Color4 HoverColor { get; set; } = new Color4(0.5f, 0.5f, 1.05f, 1f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1f);
        public Color4 ListBackgroundColor { get; set; } = new Color4(0.1f, 0.1f, 0.1f, 0.95f);

        public event Action<DropDown, int> OnSelectionChanged;

        public DropDown(float x, float y, float width, float height)
        {
            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);
            Pivot = new Vector2(0.5f, 0.5f);
            AllowHover = true; // сам дропдаун должен получать события (для закрытия по клику вне)

            // Создаём главную кнопку
            _mainButton = new Button(x, y, width, height, MainButtonTexture, PlaceholderText)
            {
                Parent = this,
                Pivot = new Vector2(0.5f, 0.5f),
                FontSize = FontSize,
                TextColor = TextColor,
                TextAlign = TextAlign,
                NormalColor = NormalColor,
                HoverColor = HoverColor,
                PressColor = PressColor,
                AllowHover = true,
                Tag = "NoHoverSound" // чтобы не пищало при наведении внутри списка
            };
            _mainButton.OnClick += OnMainButtonClick;
            AddChild(_mainButton);

            // Фон выпадающего списка
            _listBackground = new SpriteObject(0, 0, 0, 1, 1)
            {
                Parent = this,
                Active = false,
                Pivot = new Vector2(0.5f, 0f), // растягиваем вниз
                Color = ListBackgroundColor,
                AllowHover = false
            };
            if (!string.IsNullOrEmpty(ListBackgroundTexture))
                _listBackground._textureId = TextureManager.GetTexture(ListBackgroundTexture);
            AddChild(_listBackground);

            // ScaleMultiply у дропдауна по умолчанию 1, но может меняться извне
        }

        private void OnMainButtonClick()
        {
            ToggleExpanded();
        }

        public void ToggleExpanded()
        {
            if (_isExpanded)
                Collapse();
            else
                Expand();
        }

        public void Expand()
        {
            if (_isExpanded) return;
            _isExpanded = true;

            RebuildItemButtons();

            _listBackground.Active = true;
            foreach (var btn in _itemButtons)
                btn.Active = true;

            // Анимация появления
        }

        public void Collapse()
        {
            if (!_isExpanded) return;
            _isExpanded = false;

            _listBackground.Active = false;
            foreach (var btn in _itemButtons)
                btn.Active = false;
        }

        private void RebuildItemButtons()
        {
            // Удаляем старые кнопки
            foreach (var btn in _itemButtons)
                RemoveChild(btn);
            _itemButtons.Clear();

            if (_items.Count == 0) return;

            // Вычисляем высоту пункта
            float itemHeight = ItemHeight;
            if (itemHeight <= 0)
            {
                // Используем высоту строки шрифта с учётом ScaleMultiply
                float baseScale = TR.GetScaleFromFontSize(FontSize);
                // Учитываем, что у кнопки будет свой ScaleMultiply = 1, но родительский (наш) влияет
                float effectiveScaleMult = EffectiveScaleMultiply;
                float textHeight = TR.CurrentFont.LineHeight * baseScale * effectiveScaleMult;
                itemHeight = textHeight + ItemPadding.Y * 2 * effectiveScaleMult;
            }

            float totalHeight = itemHeight * _items.Count;
            float panelHeight = Math.Min(totalHeight, MaxDropHeight);
            float panelWidth = Scale.X * EffectiveScaleMultiply; // ширина кнопки с учётом ScaleMultiply

            // Позиционируем фон: прямо под кнопкой, с учётом pivot (0.5,0)
            _listBackground.Position = new Vector2(Position.X, Position.Y + Scale.Y * 0.5f * EffectiveScaleMultiply);
            _listBackground.Scale = new Vector2(panelWidth, panelHeight);
            _listBackground.ScaleMultiply = 1f; // не удваиваем

            float startY = _listBackground.Position.Y + itemHeight * 0.5f; // центр первого пункта по Y

            for (int i = 0; i < _items.Count; i++)
            {
                float yPos = startY + i * itemHeight;
                var itemBtn = new Button(
                    _listBackground.Position.X,
                    yPos,
                    panelWidth,
                    itemHeight,
                    MainButtonTexture,
                    _items[i])
                {
                    Parent = this, // важно: родитель DropDown, чтобы ScaleMultiply наследовался
                    FontSize = FontSize,
                    TextColor = TextColor,
                    TextAlign = TextAlign,
                    NormalColor = NormalColor,
                    HoverColor = HoverColor,
                    PressColor = PressColor,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Active = true,
                    AllowHover = true,
                    Tag = "NoHoverSound"
                };

                int index = i;
                itemBtn.OnClick += () => OnItemClicked(index);
                AddChild(itemBtn);
                _itemButtons.Add(itemBtn);
            }
        }

        private void OnItemClicked(int index)
        {
            SelectedIndex = index;
            Collapse();
        }

        // === Публичные свойства ===

        public List<string> Items
        {
            get => _items;
            set
            {
                _items = value ?? new List<string>();
                if (_isExpanded)
                    RebuildItemButtons();
                if (_selectedIndex >= _items.Count)
                    SelectedIndex = -1;
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1 || value >= _items.Count) return;
                if (_selectedIndex == value) return;

                _selectedIndex = value;
                UpdateMainButtonText();
                OnSelectionChanged?.Invoke(this, _selectedIndex);
            }
        }

        public string SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

        private void UpdateMainButtonText()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                _mainButton.Text = _items[_selectedIndex];
            else
                _mainButton.Text = PlaceholderText;
        }

        // === Обработка кликов вне области ===

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            if (_isExpanded)
            {
                if (mouse.IsButtonPressed(MouseButton.Left))
                {
                    Vector2 mouseWorld = new Vector2(mouse.X / CanvasScale.X, mouse.Y / CanvasScale.Y);
                    bool insideMain = IsPointInsideMainButton(mouseWorld);
                    bool insideList = IsPointInsideList(mouseWorld);
                    if (!insideMain && !insideList)
                    {
                        Collapse();
                    }
                }
            }
        }

        private bool IsPointInsideMainButton(Vector2 worldPos)
        {
            var bounds = _mainButton.GetDesignBounds();
            float left = bounds.designLeft;
            float right = bounds.designLeft + bounds.effWidth;
            float top = bounds.designTop;
            float bottom = bounds.designTop + bounds.effHeight;
            return worldPos.X >= left && worldPos.X <= right && worldPos.Y >= top && worldPos.Y <= bottom;
        }

        private bool IsPointInsideList(Vector2 worldPos)
        {
            if (!_listBackground.Active) return false;
            var bounds = _listBackground.GetDesignBounds();
            float left = bounds.designLeft;
            float right = bounds.designLeft + bounds.effWidth;
            float top = bounds.designTop;
            float bottom = bounds.designTop + bounds.effHeight;
            return worldPos.X >= left && worldPos.X <= right && worldPos.Y >= top && worldPos.Y <= bottom;
        }

        // Переопределяем SetHover, чтобы главная кнопка тоже получала hover (для анимации)
        public override void SetHover(bool hover)
        {
            base.SetHover(hover);
            _mainButton.SetHover(hover);
        }
    }
}