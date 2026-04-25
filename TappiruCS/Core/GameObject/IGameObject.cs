using OpenTK.Mathematics;

namespace TappiruCS.Core.GameObject
{
    public interface IGameObject : IUpdatable, IRenderable
    {
        Vector2 WorldPosition { get; set; }
        Vector2 Scale { get; set; }
        float Opacity { get; set; }
        int Layer { get; set; }
        bool Active { get; set; }
    }
}
