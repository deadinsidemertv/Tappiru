using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.Core
{
    public interface IRenderable
    {
        void Draw(Matrix4 projection);
    }
}
