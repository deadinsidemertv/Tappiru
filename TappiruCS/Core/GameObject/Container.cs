using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace TappiruCS.Core.GameObject
{
    public class Container:GameObject
    {
        public Container(float x, float y)
        {
            LocalPosition = new Vector2(x,y);
        }

        public void Include(GameObject obj)
        {
            AddChild(obj);
        }
    }
}
