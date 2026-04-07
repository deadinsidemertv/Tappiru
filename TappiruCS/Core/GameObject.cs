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
            public bool AutoScale { get; set; } = true;          //Разрешить автоскейл

            public float ScaleMultiply = 1f;
            public bool IsHovered { get; set; } = false;

            public bool AllowHover = true;
            public Vector2 CanvasScale { get; set; } = new Vector2(1f, 1f);

            public string Tag { get; set; } = "";

            // === PIVOT SYSTEM ===
            public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f); // по умолчанию — центр

            public float _baseScaleMultiply = -1f;


            // ====================== PIVOT HELPERS ======================
            public (float designLeft, float designTop, float effWidth, float effHeight) GetDesignBounds()
            {
                float effWidth = Scale.X * ScaleMultiply;
                float effHeight = Scale.Y * ScaleMultiply;
                float pivotOffsetX = effWidth * Pivot.X;
                float pivotOffsetY = effHeight * Pivot.Y;

                float designLeft = Position.X - pivotOffsetX;
                float designTop = Position.Y - pivotOffsetY;

                return (designLeft, designTop, effWidth, effHeight);
            }

            // Базовая реализация (для большинства объектов)
            public virtual void Update(double deltaTime)
            {
               
            }

            public virtual void Update(double deltaTime, MouseState mouse)
            {
                Update(deltaTime);
            }

            public abstract void Draw(Matrix4 projection);

            public virtual void SetHover(bool hover)
            {
                IsHovered = hover;
            }

            // Теперь IsPointInside учитывает pivot
            public virtual bool IsPointInside(float worldX, float worldY)
            {
                var (left, top, effWidth, effHeight) = GetDesignBounds();
                float right = left + effWidth;
                float bottom = top + effHeight;

                return worldX >= left && worldX <= right &&
                       worldY >= top && worldY <= bottom;
            }

            
            

            
        }
    }
}