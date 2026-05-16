// Core/GameObject/GameObject.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Render;
using TappiruCS.Render.Audio;
using TappiruCS.Render.Text;
using TappiruCS.Render.Text.FreeType;
using TappiruCS.UI;

namespace TappiruCS.Core.GameObject
{
    public abstract class GameObject : IGameObject
    {
        //["Debug"]//
        public bool Debug = false;

        public Vector2 WorldPosition { get; set; } = Vector2.Zero;
        public Vector2 LocalPosition { get; set; } = Vector2.Zero;

        public string Description { get; set; } = string.Empty;
        public Vector2 Scale { get; set; } = Vector2.One;
        public float Opacity { get; set; } = 1f;
        public int Layer { get; set; } = 0;
        public bool Active { get; set; } = true;
        public bool AutoScale { get; set; } = true;

        public float ScaleMultiply { get; set; } = 1f;
        public bool IsHovered { get; set; } = false;

        public bool AllowHover = true;
        public Vector2 CanvasScale { get; set; } = new Vector2(1f, 1f);

        public string Tag { get; set; } = "";

        // === PIVOT SYSTEM ===
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);

        // "Родной" ScaleMultiply объекта до умножения на родителя.
        // -1f = ещё не инициализирован (запомним при первом Update).
        public float _baseScaleMultiply = -1f;

        // ====================== ИЕРАРХИЯ ======================
        public GameObject? Parent { get; set; } = null;

        protected readonly List<GameObject> _children = new List<GameObject>();
        public IReadOnlyList<GameObject> Children => _children.AsReadOnly();

        public RenderContext Context { get; protected set; }

        protected SpriteBatch SB => Context?.SpriteBatch;
        protected Game Game => Context?.Game;
        protected AudioManager Audio => Context?.Audio;

        protected FreeTypeRender FT => FontManager._defaultFont ?? FontManager.Get("UI")!;

        internal void SetRenderContext(RenderContext context)
        {
            Context = context;
            OnContextSet();

            foreach (var child in _children)
                child.SetRenderContext(context);
        }

        protected virtual void OnContextSet() { }

        public virtual void Update(double deltaTime, MouseState mouse)
        {
            if (!Active || Context == null)
                return;

            if (Parent == null)
                WorldPosition = LocalPosition;

            foreach (var child in _children)
            {
                if (!child.Active) continue;

                child.CanvasScale = CanvasScale;

                // ── Каскад ScaleMultiply ──────────────────────────────────────────
                // При первом Update запоминаем "родной" ScaleMultiply ребёнка.
                // Это позволяет объекту иметь собственный масштаб (например TextObject
                // с FontSize-зависимым scale), который затем умножается на цепочку
                // родителей — точно так же, как матрица трансформации.
                if (child._baseScaleMultiply < 0f)
                    child._baseScaleMultiply = child.ScaleMultiply;

                child.ScaleMultiply = child._baseScaleMultiply * ScaleMultiply;
                // ─────────────────────────────────────────────────────────────────

                child.WorldPosition = child.GetWorldPosition();
                child.Update(deltaTime, mouse);
            }
        }

        public virtual void Draw(Matrix4 projection)
        {
            if (!Active || Context == null || SB == null)
                return;

            // Ищем ClippingMask среди детей
            ClippingMask clip = null;
            foreach (var child in _children)
            {
                if (child is ClippingMask cm && child.Active)
                {
                    clip = cm;
                    break;
                }
            }

            // Если маска есть — включаем клиппинг перед рисованием детей
            if (clip != null)
                clip.BeginClip(projection);

            foreach (var child in _children)
            {
                if (child.Active)
                    child.Draw(projection);
            }

            // Выключаем клиппинг после всех детей
            if (clip != null)
                clip.EndClip(projection);
        }

        // ====================== ДЕТИ ======================

        public void AddChild(GameObject child)
        {
            if (child == null || _children.Contains(child)) return;
            child.Parent = this;
            _children.Add(child);

            if (Context != null)
                child.SetRenderContext(Context);
        }

        public void RemoveChild(GameObject child)
        {
            if (_children.Remove(child))
                child.Parent = null;
        }

        // ====================== Hover и Bounds ======================

        public virtual void SetHover(bool hover)
        {
            if (IsHovered == hover) return;

            bool wasHovered = IsHovered;
            IsHovered = hover;

            if (hover && !wasHovered)
                OnHoverEnter?.Invoke(this);
            else if (!hover && wasHovered)
                OnHoverExit?.Invoke(this);
        }

        public virtual bool IsPointInside(float worldX, float worldY)
        {
            var (left, top, effWidth, effHeight) = GetDesignBounds();
            float right = left + effWidth;
            float bottom = top + effHeight;

            return worldX >= left && worldX <= right &&
                   worldY >= top && worldY <= bottom;
        }

        public virtual (float designLeft, float designTop, float effWidth, float effHeight) GetDesignBounds()
        {
            // ScaleMultiply уже каскадный после Update, поэтому используем его напрямую.
            // EffectiveScaleMultiply оставлен как запасной вариант для случаев,
            // когда GetDesignBounds вызывается до первого Update (например в редакторе).
            float mul = _baseScaleMultiply >= 0f ? ScaleMultiply : EffectiveScaleMultiply;

            float effWidth = Scale.X * mul;
            float effHeight = Scale.Y * mul;
            float pivotOffsetX = effWidth * Pivot.X;
            float pivotOffsetY = effHeight * Pivot.Y;

            float designLeft = WorldPosition.X - pivotOffsetX;
            float designTop = WorldPosition.Y - pivotOffsetY;

            return (designLeft, designTop, effWidth, effHeight);
        }

        // Оставляем как fallback — используется до первого Update или в редакторе.
        public float EffectiveScaleMultiply
        {
            get
            {
                float mul = ScaleMultiply;
                var current = Parent;
                while (current != null)
                {
                    mul *= current.ScaleMultiply;
                    current = current.Parent;
                }
                return mul;
            }
        }

        public virtual void SetHoverRecursive(GameObject? targetHover)
        {
            bool shouldBeHovered = (this == targetHover);

            if (IsHovered != shouldBeHovered)
                SetHover(shouldBeHovered);

            foreach (var child in _children)
                child.SetHoverRecursive(targetHover);
        }

        public virtual void CollectHoverCandidates(float virtualX, float virtualY,
                                           ref GameObject top, ref int topLayer)
        {
            if (!Active) return;

            bool isInside = IsPointInside(virtualX, virtualY);

            if (AllowHover && isInside && Layer >= topLayer)
            {
                topLayer = Layer;
                top = this;
            }

            foreach (var child in _children)
                child.CollectHoverCandidates(virtualX, virtualY, ref top, ref topLayer);
        }

        public Vector2 GetWorldPosition()
        {
            if (Parent == null) return LocalPosition;
            return Parent.WorldPosition + LocalPosition;
        }

        public event Action<GameObject> OnHoverEnter;
        public event Action<GameObject> OnHoverExit;
    }
}