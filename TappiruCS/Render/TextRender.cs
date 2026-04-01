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
        private readonly Dictionary<char, float> charAdvance = new Dictionary<char, float>();   // ширина
        private readonly Dictionary<char, float> charYOffset = new Dictionary<char, float>();   // ← НОВОЕ: сдвиг по Y

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

            string chars = "0123456789абвгдеёжзийклмнопрстуфхцчшщъыьэюя,./!?><;:'[]{}\\|@#$%^qwertyuiopasdfghjklzxcvbnm ";
            for (int i = 0; i < chars.Length; i++)
                charToIndex[chars[i]] = i;

            BuildDefaultAdvancesAndOffsets();
        }

        private void BuildDefaultAdvancesAndOffsets()
        {
            float defaultAdvance = charWidth * 0.88f;

            foreach (var pair in charToIndex)
            {
                char c = pair.Key;
                float advance = defaultAdvance;
                float yOffset = 0f;                     // по умолчанию без сдвига

                switch (c)
                {
                    case '.':
                    case ',':
                        advance = charWidth * 0.25f;
                        yOffset = charHeight * 0.48f;   // ← главная строчка! опускаем точку вниз
                        break;

                    case ':':
                    case ';':
                    case '!':
                        advance = charWidth * 0.20f;
                        yOffset = charHeight * 0.1f;
                        break;

                    case ' ':
                        advance = charWidth * 0.62f;
                        break;

                    case 'i':
                    case 'l':
                    //case '1':
                    case 't':
                        advance = charWidth * 0.58f;
                        break;

                    case 'm':
                    case 'w':
                        advance = charWidth * 1.08f;
                        break;

                    case '\\':
                    case '/':
                        advance = charWidth * 1f;
                        break;

                }

                charAdvance[c] = advance;
                charYOffset[c] = yOffset;
            }
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
        public void DrawString(string text, float x, float y, float scale, float spacing,
                               float r, float g, float b, float a, Matrix4 projection)
        {
            if (string.IsNullOrEmpty(text)) return;

            float currentX = x;

            foreach (char original in text)
            {
                char c = char.ToLower(original);

                if (charToIndex.TryGetValue(c, out int index))
                {
                    var uv = GetUV(index);

                    float glyphWidth = charWidth * scale;
                    float advance = (charAdvance.TryGetValue(c, out float adv) ? adv : charWidth * 0.88f)
                                    * scale * spacing;

                    // ← Вот здесь применяем вертикальный сдвиг
                    float yOffset = charYOffset.TryGetValue(c, out float offset) ? offset * scale : 0f;
                    float drawY = y + yOffset;

                    spriteBatch.Draw(textureId, currentX, drawY, glyphWidth, charHeight * scale,
                                     uv.u1, uv.v1, uv.u2, uv.v2, r, g, b, a, projection);

                    currentX += advance;
                }
                else
                {
                    currentX += charWidth * scale * spacing * 0.88f;
                }
            }
        }

        // Перегрузки и CalculateTextWidth оставляем почти без изменений (только spacing по умолчанию чуть увеличил)
        public void DrawString(string text, float x, float y, float scale, float spacing,
                               float r, float g, float b, float a, Matrix4 projection,
                               TextAlign align = TextAlign.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            float textWidth = CalculateTextWidth(text, scale, spacing);

            float startX = align switch
            {
                TextAlign.Center => x - textWidth / 2f,
                TextAlign.Right => x - textWidth,
                _ => x
            };

            DrawString(text, startX, y, scale, spacing, r, g, b, a, projection);
        }

        public void DrawString(string text, float x, float y, float scale,
                               float r, float g, float b, float a, Matrix4 projection,
                               TextAlign align = TextAlign.Left)
        {
            DrawString(text, x, y, scale, 0.88f, r, g, b, a, projection, align);
        }

        public float CalculateTextWidth(string text, float scale, float spacing = 0.88f)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            float total = 0f;
            foreach (char original in text)
            {
                char c = char.ToLower(original);
                float advance = charAdvance.TryGetValue(c, out float adv) ? adv : charWidth * 0.88f;
                total += advance * scale * spacing;
            }
            return total;
        }

        public float CalculateTextWidth(string text, float scale)
        {
            return CalculateTextWidth(text, scale, 0.88f);
        }
    }
}