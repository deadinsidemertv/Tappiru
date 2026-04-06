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
            public Vector2 CanvasScale { get; set; } = new Vector2(1f, 1f);

            // === PIVOT SYSTEM ===
            public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f); // по умолчанию — центр

            public float _originalScaleMultiply;

            private readonly List<Tween> _tweens = new List<Tween>();

            // ====================== PIVOT HELPERS ======================
            protected (float designLeft, float designTop, float effWidth, float effHeight) GetDesignBounds()
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
                for (int i = _tweens.Count - 1; i >= 0; i--)
                {
                    var tween = _tweens[i];
                    tween.Update(deltaTime);

                    if (tween.IsFinished)
                        _tweens.RemoveAt(i);
                }
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

            // Методы анимаций (без изменений)
            private float _baseScaleMultiply = -1f;

            public Tween ScaleAnim(float multiplier, float duration = 0.2f)
            {
                if (_baseScaleMultiply < 0f)
                    _baseScaleMultiply = ScaleMultiply;

                float target = _baseScaleMultiply * multiplier;

                RemoveTweensOfType(TweenType.ScaleMultiply);

                if (Math.Abs(ScaleMultiply - target) < 0.005f)
                    return null;

                var tween = new Tween(this, TweenType.ScaleMultiply, target, duration);
                _tweens.Add(tween);
                return tween;
            }

            public Tween ResetScaleMultiply(float duration = 0.25f)
            {
                if (_baseScaleMultiply < 0f)
                    _baseScaleMultiply = ScaleMultiply;

                return ScaleAnim(1.0f, duration);
            }

            private void RemoveTweensOfType(TweenType type)
            {
                for (int i = _tweens.Count - 1; i >= 0; i--)
                {
                    if (_tweens[i].Type == type)
                        _tweens.RemoveAt(i);
                }
            }

            public void ClearTweens() => _tweens.Clear();
        }
    }
}