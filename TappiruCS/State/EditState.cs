using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.State
{
    internal class EditState : IGameState
    {
        public void OnEnter() { }
        public void OnExit() { }
        public void Update(double currentTime) { }
        public void Render(Matrix4 projection) { }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }
    }
}
