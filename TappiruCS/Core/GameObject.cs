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
            public bool AutoScale { get; set; } = true;

            public float ScaleMultiply = 1f;
            public bool IsHovered { get; set; } = false;

            public bool AllowHover = true;

            public Vector2 CanvasScale { get; set; } = new Vector2(1f,1f);

            // Базовая реализация (для большинства объектов)
            public virtual void Update(double deltaTime) { }

            // Перегрузка для объектов, которым нужна мышь (Button и т.д.)
            public virtual void Update(double deltaTime, MouseState mouse)
            {
                Update(deltaTime); // по умолчанию вызываем обычный Update
            }

            public abstract void Draw(Matrix4 projection);

            public virtual void SetHover(bool hover)
            {
                IsHovered = hover;
            }

            public virtual bool IsPointInside(float worldX, float worldY)  // worldX/Y — уже в дизайн-координатах (виртуальных)
            {
                // Учитываем ScaleMultiply для всего объекта
                float scaledPosX = Position.X * ScaleMultiply;
                float scaledPosY = Position.Y * ScaleMultiply;
                float scaledWidth = Scale.X * ScaleMultiply;
                float scaledHeight = Scale.Y * ScaleMultiply;

                float left = scaledPosX;
                float right = scaledPosX + scaledWidth;
                float top = scaledPosY;
                float bottom = scaledPosY + scaledHeight;

                return worldX >= left && worldX <= right &&
                       worldY >= top && worldY <= bottom;
            }
        }
    }
}
