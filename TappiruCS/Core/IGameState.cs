using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

namespace TappiruCS
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
