using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.Core
{
    namespace TappiruCS.Core
    {
        public abstract class GameObject : IGameObject
        {
            public Vector2 Position { get; set; } = Vector2.Zero;
            public Vector2 Scale { get; set; } = Vector2.One;
            public float Rotation { get; set; } = 0f;
            public int Layer { get; set; } = 0;
            public bool Active { get; set; } = true;

            public Vector2 CanvasScale { get; set; } = new Vector2(1f,1f);

            // Базовая реализация (для большинства объектов)
            public virtual void Update(double deltaTime) { }

            // Перегрузка для объектов, которым нужна мышь (Button и т.д.)
            public virtual void Update(double deltaTime, MouseState mouse)
            {
                Update(deltaTime); // по умолчанию вызываем обычный Update
            }

            public abstract void Draw(Matrix4 projection);
        }
    }
}
