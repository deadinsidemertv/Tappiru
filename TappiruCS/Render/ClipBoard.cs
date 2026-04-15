// Clipboard.cs — исправленная версия для OpenTK 4.x
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace TappiruCS.Core
{
    public static class Clipboard
    {
        /// <summary>
        /// Копирует текст в буфер обмена
        /// </summary>
        public static void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                unsafe
                {
                    GLFW.SetClipboardString(null, text);   // null вместо Window*
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка копирования в буфер: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает текст из буфера обмена
        /// </summary>
        public static string GetText()
        {
            try
            {
                unsafe
                {
                    return GLFW.GetClipboardString(null) ?? string.Empty;   // null вместо Window*
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения из буфера: {ex.Message}");
                return string.Empty;
            }
        }
    }
}