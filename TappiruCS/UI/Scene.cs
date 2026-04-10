using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.Tween;

namespace TappiruCS.Core
{
    public class Scene
    {
        public static Scene Current { get; private set; }

        public List<GameObject> _objects = new List<GameObject>();

        public const float DesignWidth = 1920f;
        public const float DesignHeight = 1080f;

        public static Vector2 CanvasScale;

        public static Vector2 LogicMouse;

        public TweenManager TweenManager { get; } = new TweenManager();

        public void Add(GameObject obj) => _objects.Add(obj);
        public void Remove(GameObject obj) => _objects.Remove(obj);
        public void Clear() => _objects.Clear();

        public Scene()
        {
            Current = this;        // Когда создаётся сцена — она становится текущей
        }
        // Новая версия Update с MouseState
        public void Update(double deltaTime, MouseState mouse, Game _game)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;
                obj.CanvasScale = CanvasScale;
                obj.Update(deltaTime,mouse);   // вызываем перегрузку с mouse
            }


            CanvasScale = new Vector2(_game.ClientSize.X / DesignWidth,
                                      _game.ClientSize.Y / DesignHeight);
            var virtualMouse = GetVirtualMousePosition(mouse);
            UpdateHover(virtualMouse.X, virtualMouse.Y);

            LogicMouse = new Vector2(virtualMouse.X, virtualMouse.Y);

            TweenManager.Update(deltaTime);
        }
        public void Update(double deltaTime)
        {
            
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;
                obj.CanvasScale = CanvasScale;
                obj.Update(deltaTime);   // вызываем перегрузку с mouse
            }
        }

        public void Draw(Matrix4 projection)
        {
            var sorted = _objects
                .Where(o => o.Active)
                .OrderBy(o => o.Layer)
                .ToList();

            foreach (var obj in sorted)
            {
                
                obj.CanvasScale = CanvasScale;
                obj.Draw(projection);
            }
                
        }

        public void UpdateHover(float virtualMouseX, float virtualMouseY)
        {
            GameObject top = null;
            int topLayer = int.MinValue;

            // Сначала находим топовый объект
            foreach (var obj in _objects)
            {
                if (!obj.Active) continue;
                if (!obj.AllowHover) continue;

                if (obj.IsPointInside(virtualMouseX, virtualMouseY))
                {
                    if (obj.Layer > topLayer)
                    {
                        topLayer = obj.Layer;
                        top = obj;
                    }
                }
            }

            // Теперь сбрасываем hover у всех, КРОМЕ топового
            foreach (var obj in _objects)
            {
                if (obj == top)
                    obj.SetHover(true);      // сразу ставим true топовому
                else
                    obj.SetHover(false);
            }
        }

        private Vector2 GetVirtualMousePosition(MouseState mouse)
        {
            return new Vector2(
                mouse.X / CanvasScale.X,
                mouse.Y / CanvasScale.Y
            );
        }
    }
}