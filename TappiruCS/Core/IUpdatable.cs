using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TappiruCS.Core
{
    public interface IUpdatable
    {
        void Update(double deltaTime, MouseState mouse);  // перегрузка для объектов с вводом
    }
}
