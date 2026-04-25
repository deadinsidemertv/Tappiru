using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Server.Player;

namespace TappiruCS.State.SongSelector.RankingPanel
{

    public class RankingPanel : GameObject
    {
        // ── Настройки макета ──
        private const float ItemHeight = 100f;
        private const float ItemSpacing = 2f;
        private const float PanelX = 180f;   // X позиция кнопок
        private const int MaxVisible = 50;

        // ── Настройки скролла ──
        private const float ScrollSpeed = 120f;
        private const float Smoothness = 14f;
        private const float VisibleHeight = 7 * (ItemHeight + ItemSpacing);

        // ── Ширина области для hit-test драга ──
        private const float PanelWidth = 700f;

        // ── Скролл ──
        private float _targetOffsetY = 0f;
        private float _currentOffsetY = 0f;

        // ── Drag ──
        private bool _isDragging = false;
        private float _lastMouseY = 0f;

        // ── Данные ──
        private readonly IScoreProvider _provider;
        private readonly List<ScoreButton> _buttons = new();

        // ── Hover-анимация (как в ScrollList) ──
        private float[] _hoverOffsetX = Array.Empty<float>();
        private float[] _hoverOffsetY = Array.Empty<float>();
        private int _hoveredIndex = -1;

        public RankingPanel(float x, float y, IScoreProvider provider)
        {
            LocalPosition = new Vector2(x, y);
            _provider = provider;
            Opacity = 0.5f;
        }

        // ─────────────────────────────────────────────
        //  Публичный API
        // ─────────────────────────────────────────────

        public void Refresh(string mapHash)
        {
            ClearButtons();
            ResetScroll();

            var scores = _provider.GetScores(mapHash);

            if (scores == null || scores.Count == 0)
            {
                ShowEmptyPlaceholder();
                return;
            }

            int count = Math.Min(scores.Count, MaxVisible);
            for (int i = 0; i < count; i++)
            {
                AddScoreButton(scores[i], rank: i + 1);
            }
            Console.WriteLine($"[RankingPanel] Loaded {_buttons.Count} scores, MaxVisible={MaxVisible}");
        }

        /// <summary>Прокрутка от колёсика мыши.</summary>
        public void Scroll(float deltaY)
        {
            Console.WriteLine($"[RankingPanel] Scroll delta: {deltaY}");
            _targetOffsetY -= deltaY * ScrollSpeed;
            ClampScroll();
            Console.WriteLine($"[RankingPanel] After clamp: {_targetOffsetY}");
        }

        // ─────────────────────────────────────────────
        //  Overrides
        // ─────────────────────────────────────────────

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);



            float dt = (float)deltaTime;

            // Плавный скролл
            _currentOffsetY = MathHelper.Lerp(_currentOffsetY, _targetOffsetY, Smoothness * dt);

            // Hover-анимация
            UpdateHoverOffsets(dt);

            // Позиции кнопок
            ApplyButtonPositions();

            // Перетаскивание мышью
            HandleDragging(mouse);
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection); // рисует всех Children (кнопки)
        }

        // ─────────────────────────────────────────────
        //  Построение кнопок
        // ─────────────────────────────────────────────

        private void AddScoreButton(PlayerScore score, int rank)
        {
            // Y пока 0 — реальная позиция выставляется в ApplyButtonPositions
            var button = new ScoreButton(0, 0f, score)
            {
                Layer = 3,
                ScaleMultiply = 1.0f,
                Opacity = 0.5f,
            };

            button.SetRank(rank);

            // Захватываем score в closure чтобы не было проблем с индексом
            var captured = score;
            button.OnClick += () => OnScoreClicked?.Invoke(captured);

            // Hover-события для анимации
            int index = _buttons.Count;
            button.OnHoverEnter += _ => _hoveredIndex = index;
            button.OnHoverExit += _ => { if (_hoveredIndex == index) _hoveredIndex = -1; };

            _buttons.Add(button);
            AddChild(button);

            // Расширяем массивы анимации
            Array.Resize(ref _hoverOffsetX, _buttons.Count);
            Array.Resize(ref _hoverOffsetY, _buttons.Count);
        }

        private void ShowEmptyPlaceholder()
        {
            var empty = new PlayerScore
            {
                PlayerName = "Нет результатов",
                _score = 0,
                _accuraci = 0,
                _maxCobmo = 0,
            };

            var btn = new ScoreButton(PanelX, WorldPosition.Y, empty)
            {
                Layer = 3,
                LocalPosition = new Vector2(PanelX, WorldPosition.Y),
                Opacity = 0.5f,
                Parent = this,
            };

            btn.Avatar.Active = false;
            btn.Grade.Active = false;

            _buttons.Add(btn);
            AddChild(btn);

            Array.Resize(ref _hoverOffsetX, _buttons.Count);
            Array.Resize(ref _hoverOffsetY, _buttons.Count);
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons)
                RemoveChild(btn);

            _buttons.Clear();
            _hoveredIndex = -1;
            _hoverOffsetX = Array.Empty<float>();
            _hoverOffsetY = Array.Empty<float>();
        }

        // ─────────────────────────────────────────────
        //  Анимация и позиционирование
        // ─────────────────────────────────────────────

        private void UpdateHoverOffsets(float dt)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                float targetX = 0f;
                float targetY = 0f;

                if (_hoveredIndex != -1)
                {
                    if (i == _hoveredIndex)
                        targetX = -60f;            // выделенная кнопка — уходит влево
                    else if (i < _hoveredIndex)
                        targetY = -20f;            // кнопки выше — вверх
                    else
                        targetY = 20f;             // кнопки ниже — вниз
                }

                _hoverOffsetX[i] = MathHelper.Lerp(_hoverOffsetX[i], targetX, 8f * dt);
                _hoverOffsetY[i] = MathHelper.Lerp(_hoverOffsetY[i], targetY, 8f * dt);
            }
        }

        private void ApplyButtonPositions()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                float rawY = WorldPosition.Y + i * (ItemHeight + ItemSpacing) - _currentOffsetY;
                float finalX = PanelX + _hoverOffsetX[i];
                float finalY = rawY + _hoverOffsetY[i];

                _buttons[i].LocalPosition = new Vector2(finalX, finalY);
                _buttons[i].Active = IsVisible(i);
            }
        }

        private bool IsVisible(int index)
        {
            float top = index * (ItemHeight + ItemSpacing) - _currentOffsetY;
            // небольшой буфер чтобы кнопки не мигали на границе
            return top < VisibleHeight + 200f && top + ItemHeight > -200f;
        }

        // ─────────────────────────────────────────────
        //  Скролл и драг
        // ─────────────────────────────────────────────

        private void HandleDragging(MouseState mouse)
        {
            bool overPanel = IsMouseOverPanel(mouse.X, mouse.Y);

            if (mouse.IsButtonDown(MouseButton.Left) && overPanel)
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
                    _lastMouseY = mouse.Y;
                    ClampScroll();
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        private bool IsMouseOverPanel(float mouseX, float mouseY)
        {
            float left = WorldPosition.X - PanelWidth * 0.5f;
            float right = left + PanelWidth;
            float top = WorldPosition.Y;
            float bottom = top + VisibleHeight;

            return mouseX >= left && mouseX <= right &&
                   mouseY >= top && mouseY <= bottom;
        }

        private void ClampScroll()
        {
            float contentHeight = _buttons.Count * (ItemHeight + ItemSpacing);
            float maxScroll = Math.Max(0f, contentHeight - VisibleHeight);
            _targetOffsetY = Math.Clamp(_targetOffsetY, 0f, maxScroll);
        }

        private void ResetScroll()
        {
            _targetOffsetY = 0f;
            _currentOffsetY = 0f;
        }

        // ─────────────────────────────────────────────
        //  События
        // ─────────────────────────────────────────────
        public event Action<PlayerScore> OnScoreClicked;
    }
}