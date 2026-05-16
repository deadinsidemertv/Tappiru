using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Tween;
using TappiruCS.UI;

namespace TappiruCS.Core.GameObject
{
    public class Scene
    {
        public static Scene Current { get; private set; }

        public MouseNotification? MouseNotification { get; set; }

        public RenderContext RenderContext { get; private set; }

        private readonly List<GameObject> _objects = new List<GameObject>();

        public IReadOnlyList<GameObject> Objects => _objects.AsReadOnly();

        public const float DesignWidth = 1920f;
        public const float DesignHeight = 1080f;

        public static Vector2 CanvasScale;

        public static Vector2 LogicMouse;

        public TweenManager TweenManager { get; } = new TweenManager();

        public void Initialize(RenderContext renderContext)
        {
            RenderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));

            foreach (var obj in _objects)
                obj.SetRenderContext(renderContext);
        }
        public void Add(GameObject obj)
        {
            if (obj == null) return;

            if (obj.Parent != null)
            {
                throw new InvalidOperationException(
                    $"Cannot add '{obj.GetType().Name}' directly to Scene. " +
                    $"It already has a parent. Use parent.AddChild() instead.");
            }

            if (RenderContext != null)
                obj.SetRenderContext(RenderContext);

            if (!_objects.Contains(obj))
                _objects.Add(obj);
        }
        public void Remove(GameObject obj) => _objects.Remove(obj);
        public void Clear() => _objects.Clear();

        public Scene()
        {
            Current = this;
            var notification = new MouseNotification(this);
            notification.Active = false;  // пока не показываем
            Add(notification);
            MouseNotification = notification;
        }
        public void Update(double deltaTime, MouseState mouse, Game _game)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;

                obj.CanvasScale = CanvasScale;
                obj.Update(deltaTime, mouse);
            }

            CanvasScale = new Vector2(_game.ClientSize.X / DesignWidth,
                                      _game.ClientSize.Y / DesignHeight);

            var virtualMouse = GetVirtualMousePosition(mouse);
            UpdateHover(virtualMouse.X, virtualMouse.Y);

            LogicMouse = virtualMouse;
            TweenManager.Update(deltaTime);
        }
        public void Update(double deltaTime,MouseState mouse)
        {
            
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;
                obj.CanvasScale = CanvasScale;
                obj.Update(deltaTime,mouse);   // вызываем перегрузку с mouse
            }
        }

        public void Draw(Matrix4 projection)
        {
            var sorted = _objects
                .Where(o => o.Active)
                .OrderBy(o => o.Layer)
                .ToList();

            foreach (var root in sorted)
            {
                root.CanvasScale = CanvasScale;
                root.Draw(projection);
            }

        }

        public void UpdateHover(float virtualMouseX, float virtualMouseY)
        {
            GameObject top = null;
            int topLayer = int.MinValue;

            foreach (var root in _objects)
            {
                root.CollectHoverCandidates(virtualMouseX, virtualMouseY, ref top, ref topLayer);
            }

            foreach (var root in _objects)
            {
                root.SetHoverRecursive(top);
            }

            if (MouseNotification != null)
            {
                if (top != null && !string.IsNullOrEmpty(top.Description))
                {
                    MouseNotification.text = top.Description;
                    MouseNotification.Active = true;
                }
                else
                {
                    MouseNotification.Active = false;
                }
            }

        }

        public Vector2 GetVirtualMousePosition(MouseState mouse)
        {
            return new Vector2(
                mouse.X / CanvasScale.X,
                mouse.Y / CanvasScale.Y
            );
        }
    }
}