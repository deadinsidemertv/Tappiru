using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TappiruCS.Core
{
    public abstract class GameObject : IGameObject
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Scale { get; set; } = Vector2.One;
        public float Rotation { get; set; } = 0f;
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

        public float _baseScaleMultiply = -1f;

        // ====================== ИЕРАРХИЯ (МИНИМАЛЬНАЯ) ======================
        public GameObject? Parent { get; set; } = null;           // оставляем, пока нужен

        protected readonly List<GameObject> _children = new List<GameObject>();
        public IReadOnlyList<GameObject> Children => _children.AsReadOnly();

        public void AddChild(GameObject child)
        {
            if (child == null || _children.Contains(child)) return;
            child.Parent = this;
            _children.Add(child);
        }

        public void RemoveChild(GameObject child)
        {
            if (_children.Remove(child))
                child.Parent = null;
        }

        // ====================== ОБНОВЛЕНИЕ ДЕТЕЙ (главное изменение) ======================
        public virtual void Update(double deltaTime)
        {
            foreach (var child in _children)
            {
                if (!child.Active) continue;
                child.CanvasScale = CanvasScale;     // ← вот что убирает дублирование
                child.Update(deltaTime);
            }
        }

        public virtual void Update(double deltaTime, MouseState mouse)
        {
            Update(deltaTime);                       // сначала обновляем детей без mouse

            foreach (var child in _children)
            {
                if (!child.Active) continue;
                child.CanvasScale = CanvasScale;
                child.Update(deltaTime, mouse);
            }
        }

        public virtual void Draw(Matrix4 projection) 
        {
            foreach (var child in _children)
            {
                if (child.Active)
                    child.Draw(projection);
            }
        }

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

        // ====================== PIVOT HELPERS ======================
        public (float designLeft, float designTop, float effWidth, float effHeight) GetDesignBounds()
        {
            float effWidth = Scale.X * EffectiveScaleMultiply;
            float effHeight = Scale.Y * EffectiveScaleMultiply;
            float pivotOffsetX = effWidth * Pivot.X;
            float pivotOffsetY = effHeight * Pivot.Y;

            float designLeft = Position.X - pivotOffsetX;
            float designTop = Position.Y - pivotOffsetY;

            return (designLeft, designTop, effWidth, effHeight);
        }

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
            {
                SetHover(shouldBeHovered);
            }

            // Рекурсия по детям
            foreach (var child in _children)
            {
                child.SetHoverRecursive(targetHover);
            }
        }
        public virtual void CollectHoverCandidates(float virtualX, float virtualY,
                                           ref GameObject top, ref int topLayer)
        {
            if (!Active || !AllowHover)
                return;

            if (IsPointInside(virtualX, virtualY) && Layer >= topLayer)   
            {
                topLayer = Layer;
                top = this;
            }

            foreach (var child in _children)
            {
                child.CollectHoverCandidates(virtualX, virtualY, ref top, ref topLayer);
            }
        }

        public event Action<GameObject> OnHoverEnter;
        public event Action<GameObject> OnHoverExit;
    }
}