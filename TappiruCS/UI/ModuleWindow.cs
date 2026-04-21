using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core.GameObject;

namespace TappiruCS.UI
{
    public abstract class ModuleWindow
    {
        public List<GameObject> obj;
        public Scene _scene;

        public ModuleWindow(Scene scene)
        {
            _scene = scene;
            obj  = new List<GameObject>();
        }

        public virtual void Show()
        {
            foreach (GameObject o in obj)
            {
                _scene.Add(o);
            }
        }

        public virtual void Dispose()
        {
            foreach(GameObject o in obj)
            {
                _scene?.Remove(o);
            }

        }
    }
}
