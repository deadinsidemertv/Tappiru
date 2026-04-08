using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace TappiruCS.Core
{
    public interface IGameState
    {
        void OnEnter();
        void OnExit();
        void Update(double currentTime);
        void Render(Matrix4 projection);
        void HandleKeyDown(KeyboardKeyEventArgs e);
    }
}
