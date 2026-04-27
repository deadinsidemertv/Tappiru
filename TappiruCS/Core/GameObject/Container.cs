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
                    Tag = "Debug"
                };
                AddChild(_debugBackground);
            }
        }

        public Vector2 RecalculateSize()
        {
            float topmost = float.MaxValue;
            float bottommost = float.MinValue;
            float leftmost = float.MaxValue;
            float rightmost = float.MinValue;

            foreach (var child in Children)
            {
                if (child.Tag == "Debug") continue; // игнорируем отладочные объекты



                float halfHeight = child.Scale.Y / 2;
                float halfWidth = child.Scale.X / 2;

                float top = child.LocalPosition.Y - halfHeight;
                float bottom = child.LocalPosition.Y + halfHeight;
                float left = child.LocalPosition.X - halfWidth;
                float right = child.LocalPosition.X + halfWidth;

                if (top < topmost) topmost = top;
                if (bottom > bottommost) bottommost = bottom;
                if (left < leftmost) leftmost = left;
                if (right > rightmost) rightmost = right;
            }

            if (Children.Count == 0)
            {
                MaxHeight = 0;
                MaxWidth = 400;
                return Vector2.Zero;
            }

            MaxHeight = bottommost - topmost;
            MaxWidth = rightmost - leftmost;

            float centerX = (leftmost + rightmost) / 2;
            float centerY = (topmost + bottommost) / 2;

            // Обновляем отладочный фон (если он есть)
            if (_debugBackground != null)
            {
                _debugBackground.LocalPosition = new Vector2(centerX, centerY);
                _debugBackground.Scale = new Vector2(MaxWidth, MaxHeight);
            }

            Console.WriteLine($"Top: {topmost}, Bottom: {bottommost}, Height: {MaxHeight}");
            Console.WriteLine($"Left: {leftmost}, Right: {rightmost}, Width: {MaxWidth}");

            return new Vector2(centerX, centerY);
        }
    }
}