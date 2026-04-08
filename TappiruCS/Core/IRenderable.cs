using OpenTK.Mathematics;

namespace TappiruCS.Core
{
    public interface IRenderable
    {
        void Draw(Matrix4 projection);
    }
}
