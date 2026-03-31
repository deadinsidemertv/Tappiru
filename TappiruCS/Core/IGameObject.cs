using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace TappiruCS.Core
{
    public interface IGameObject : IUpdatable, IRenderable
    {
        Vector2 Position { get; set; }
        Vector2 Scale { get; set; }
        float Rotation { get; set; }
        int Layer { get; set; }
        bool Active { get; set; }
    }
}
