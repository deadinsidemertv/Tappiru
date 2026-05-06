using OpenTK.Mathematics;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.Core.GameObject
{
    public class Container : GameObject
    {
        public float MaxWidth { get; set; }
        public float MaxHeight { get; set; }

        private SpriteObject _debugBackground; // отладочный фон (красный, полупрозрачный)

        public Container(float x, float y)
        {
            LocalPosition = new Vector2(x, y);

            if (Debug)
            {
                // Создаём отладочный фон (будет обновляться в RecalculateSize)
                _debugBackground = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 100, 100)
                {
                    Color = new Color4(1f, 0f, 0f, 0.3f), // красный полупрозрачный
                    Opacity = 0.5f,
                    Tag = "Debug",
                    AllowHover = false,
                    Description = "Debug",
                };
                AddChild(_debugBackground);
            }
            AllowHover = false;
        }

        public Vector2 RecalculateSize()
        {
            if (Children.Count == 0)
            {
                MaxWidth = 400;
                MaxHeight = 150;
                return Vector2.Zero;
            }

            float topmost = float.MaxValue;
            float bottommost = float.MinValue;
            float leftmost = float.MaxValue;
            float rightmost = float.MinValue;

            foreach (var child in Children)
            {
                if (child.Tag == "Debug") continue;

                float halfW = child.Scale.X / 2f;
                float halfH = child.Scale.Y / 2f;

                float left = child.LocalPosition.X - halfW;
                float right = child.LocalPosition.X + halfW;
                float top = child.LocalPosition.Y - halfH;
                float bottom = child.LocalPosition.Y + halfH;

                if (left < leftmost) leftmost = left;
                if (right > rightmost) rightmost = right;
                if (top < topmost) topmost = top;
                if (bottom > bottommost) bottommost = bottom;
            }

            MaxWidth = Math.Max(50f, rightmost - leftmost);
            MaxHeight = Math.Max(50f, bottommost - topmost);

            // Обновляем debug фон, но БЕЗ сдвига позиции контейнера
            if (_debugBackground != null)
            {
                float centerX = (leftmost + rightmost) / 2f;
                float centerY = (topmost + bottommost) / 2f;

                _debugBackground.LocalPosition = new Vector2(centerX, centerY);
                _debugBackground.Scale = new Vector2(MaxWidth, MaxHeight);
            }

            return new Vector2((leftmost + rightmost) / 2f, (topmost + bottommost) / 2f);
        }

        public void AddControl(GameObject control, float localX, float localY)
        {
            control.LocalPosition = new Vector2(localX, localY);
            AddChild(control);
            RecalculateSize();

        }
    }
}