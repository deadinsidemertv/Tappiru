using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TappiruCS.UI.API.LocalizationLanguage
{
    public static class Localization
    {
        private static Dictionary<string, string> _currentLocale = new();
        private static string _currentLanguage = "en";

        public static string CurrentLanguage => _currentLanguage;

        public static void Initialize()
        {
            LoadLanguage("en"); // язык по умолчанию
        }
        public static string Get(string key)
        {
            if (_currentLocale.TryGetValue(key, out string value))
                return value;

            Console.WriteLine($"[Localization] Missing key: {key}");
            return $"[{key}]"; // чтобы было видно, что ключ не найден
        }

        public static string GetFontKey(string language = null)
        {
            language ??= CurrentLanguage;
            return language switch
            {
                "ru" => "UI",   // или "NotoSans"
                "en" => "Menu",
                _ => "UI"
            };
        }

        public static event Action? OnLanguageChanged;
        public static void SetLanguage(string langCode)
        {
            if (_currentLanguage == langCode) return;

            _currentLanguage = langCode;
            LoadLanguage(langCode);

            // Можно добавить событие, чтобы UI перерисовался
            OnLanguageChanged?.Invoke();
        }

        private static void LoadLanguage(string langCode)
        {
            string path = Path.Combine("Content", "Localization", $"{langCode}.json");

            if (!File.Exists(path))
            {
                Console.WriteLine($"[Localization] File not found: {path}");
                _currentLocale.Clear();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                _currentLocale = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                               ?? new Dictionary<string, string>();

                Console.WriteLine($"[Localization] Loaded {langCode} - {_currentLocale.Count} keys");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Localization] Error loading {langCode}: {ex.Message}");
            }
        }
    }
}
