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

        public void Add(GameObject obj) => _objects.Add(obj);
        public void Remove(GameObject obj) => _objects.Remove(obj);
        public void Clear() => _objects.Clear();

        // Новая версия Update с MouseState
        public void Update(double deltaTime, MouseState mouse)
        {
            for (int i = 0 ; i < _objects.Count ; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;

                obj.Update(deltaTime, mouse);   // вызываем перегрузку с mouse
            }
        }
        public void Update(double deltaTime)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                var obj = _objects[i];
                if (!obj.Active) continue;

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
                obj.Draw(projection);
        }
    }
}