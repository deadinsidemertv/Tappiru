using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.Core
{
    public interface IUpdatable
    {
        void Update(double deltaTime);                    // базовый
        void Update(double deltaTime, MouseState mouse);  // перегрузка для объектов с вводом
    }
}
