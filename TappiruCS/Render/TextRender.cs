using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace TappiruCS.Render
{
    public class TextRender
    {
        private readonly SpriteBatch spriteBatch;
        private readonly int textureId;
        public float charWidth, charHeight;
        public int cols;
        public float texWidth, texHeight;

        private readonly Dictionary<char, int> charToIndex = new Dictionary<char, int>();

        public enum TextAlign { Left, Center, Right }

        public TextRender(SpriteBatch spriteBatch, int textureId, float texWidth, float texHeight, int cols, int rows)
        {
            this.spriteBatch = spriteBatch;
            this.textureId = textureId;
            this.texWidth = texWidth;
            this.texHeight = texHeight;
            this.charWidth = texWidth / cols;
            this.charHeight = texHeight / rows;
            this.cols = cols;

            // Заполнение словаря (подправь под свой атлас, если нужно)
            string chars = "0123456789абвгlдеёжзийклмнопрстуфхцчшщъыьэюя,./!?><;:'[]{}\\|@#$%^qwertyuiopasdfghjkzxcvbnm ";
            for (int i = 0; i < chars.Length; i++)
                charToIndex[chars[i]] = i;
        }

        private (float u1, float v1, float u2, float v2) GetUV(int index)
        {
            int row = index / cols;
            int col = index % cols;
            float u1 = (col * charWidth) / texWidth;
            float v1 = (row * charHeight) / texHeight;
            float u2 = ((col + 1) * charWidth) / texWidth;
            float v2 = ((row + 1) * charHeight) / texHeight;
            return (u1, v1, u2, v2);
        }

        // ====================== ОСНОВНОЙ МЕТОД ======================
        // Рисует текст с заданным spacing (межсимвольным интервалом)
        public void DrawString(string text, float x, float y, float scale, float spacing,
                               float r, float g, float b, float a, Matrix4 projection)
        {
            if (string.IsNullOrEmpty(text)) return;

            float w = charWidth * scale;
            float currentX = x;

            foreach (char c in text)
            {
                if (charToIndex.TryGetValue(char.ToLower(c), out int index))   // ToLower если нужно
                {
                    var uv = GetUV(index);
                    spriteBatch.Draw(textureId, currentX, y, w, charHeight * scale,
                                     uv.u1, uv.v1, uv.u2, uv.v2, r, g, b, a, projection);
                }
                currentX += w * spacing;
            }
        }

        // ====================== ПЕРЕГРУЗКИ С ВЫРАВНИВАНИЕМ ======================

        // 1. Самая удобная — с spacing и выравниванием
        public void DrawString(string text, float x, float y, float scale, float spacing,
                               float r, float g, float b, float a, Matrix4 projection,
                               TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Рассчитываем реальную ширину текста с учётом spacing
            float textWidth = CalculateTextWidth(text, scale, spacing);

            float startX = x;

            switch (align)
            {
                case TextAlign.Center:
                    startX = x - textWidth / 2f;
                    break;
                case TextAlign.Right:
                    startX = x - textWidth;
                    break;
                case TextAlign.Left:
                default:
                    startX = x;
                    break;
            }

            DrawString(text, startX, y, scale, spacing, r, g, b, a, projection);
        }

        // 2. Простая перегрузка без spacing (использует коэффициент 0.77f как у тебя раньше)
        public void DrawString(string text, float x, float y, float scale,
                               float r, float g, float b, float a, Matrix4 projection,
                               TextAlign align = TextAlign.Left)
        {
            DrawString(text, x, y, scale, 0.77f, r, g, b, a, projection, align);
        }

        // ====================== ВСПОМОГАТЕЛЬНЫЙ МЕТОД ======================
        // Очень полезный метод — возвращает ширину текста
        public float CalculateTextWidth(string text, float scale, float spacing = 0.77f)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float w = charWidth * scale;
            return text.Length * w * spacing;
        }

        // Можно добавить версию без spacing
        public float CalculateTextWidth(string text, float scale)
        {
            return CalculateTextWidth(text, scale, 0.77f);
        }
    }
}