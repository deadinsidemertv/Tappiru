using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Linq;
using TappiruCS.Core.TappiruCS.Core;

namespace TappiruCS.Core
{
    public class Scene
    {
        public List<GameObject> _objects = new List<GameObject>();

        public const float DesignWidth = 1920f;
        public const float DesignHeight = 1080f;
        public Vector2 CanvasScale = new Vector2(1f,1f);

        public void Add(GameObject obj) => _objects.Add(obj);
        public void Remove(GameObject obj) => _objects.Remove(obj);
        public void Clear() => _objects.Clear();

        // Новая версия Update с MouseState
        public void Update(double deltaTime, MouseState mouse,Game _game)
        {
            for (int i = 0 ; i < _objects.Count ; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;
                obj.CanvasScale = CanvasScale;
                obj.Update(deltaTime, mouse);   // вызываем перегрузку с mouse
            }
            CanvasScale = new Vector2(_game.ClientSize.X/DesignWidth,_game.ClientSize.Y/ DesignHeight);
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
    }
}