using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TappiruCS.GameLogic
{
    public static class MapMigrator
    {
        /// <summary>
        /// Однократная миграция всех карт: 
        /// - Копирует text -> transription (если transription пуст)
        /// - Приводит к нижнему регистру
        /// - Удаляет все знаки препинания
        /// </summary>
        /// <param name="rootMapsFolder">Корневая папка с картами</param>
        /// <returns>Количество обновлённых файлов</returns>
        public static int MigrateAllMaps(string rootMapsFolder)
        {
            if (!Directory.Exists(rootMapsFolder))
            {
                Console.WriteLine($"[MapMigrator] Папка не найдена: {rootMapsFolder}");
                return 0;
            }

            string[] tappFiles = Directory.GetFiles(rootMapsFolder, "*.tapp", SearchOption.AllDirectories);
            Console.WriteLine($"[MapMigrator] Найдено .tapp файлов: {tappFiles.Length}");

            int updatedCount = 0;
            int errorCount = 0;

            foreach (string tappPath in tappFiles)
            {
                try
                {
                    string json = File.ReadAllText(tappPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    };

                    using JsonDocument doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("events", out JsonElement eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
                        continue;

                    // Преобразуем JSON в изменяемый Dictionary
                    var mapDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                    if (mapDict == null) continue;

                    bool anyChanged = false;
                    var newEvents = new List<object>();

                    foreach (var ev in eventsElement.EnumerateArray())
                    {
                        var evDict = JsonSerializer.Deserialize<Dictionary<string, object>>(ev.GetRawText(), options);
                        if (evDict == null)
                        {
                            newEvents.Add(ev);
                            continue;
                        }

                        string text = evDict.ContainsKey("text") ? evDict["text"]?.ToString() : null;
                        string trans = evDict.ContainsKey("transription") ? evDict["transription"]?.ToString() : null;

                        // Если транскрипция отсутствует или пуста – копируем из text, чистим и приводим к нижнему регистру
                        if (string.IsNullOrEmpty(trans) && !string.IsNullOrEmpty(text))
                        {
                            string cleaned = CleanTranscription(text);
                            evDict["transription"] = cleaned;
                            anyChanged = true;
                        }

                        newEvents.Add(evDict);
                    }

                    if (!anyChanged) continue;

                    mapDict["events"] = newEvents;

                    var saveOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string updatedJson = JsonSerializer.Serialize(mapDict, saveOptions);
                    File.WriteAllText(tappPath, updatedJson, Encoding.UTF8);
                    updatedCount++;
                    Console.WriteLine($"[MapMigrator] Обновлён: {Path.GetFileName(Path.GetDirectoryName(tappPath))}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"[MapMigrator] Ошибка в {tappPath}: {ex.Message}");
                }
            }

            Console.WriteLine($"[MapMigrator] Готово. Обновлено: {updatedCount}, Ошибок: {errorCount}");
            return updatedCount;
        }

        /// <summary>
        /// Удаляет все знаки препинания и приводит строку к нижнему регистру.
        /// </summary>
        private static string CleanTranscription(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Удаляем все знаки препинания (категория Punctuation) и символы (Symbols) – пунктуация, скобки, дефисы и т.д.
            // Оставляем буквы (включая латиницу, кириллицу, японские каны? но транскрипция обычно латиница), цифры и пробелы.
            var cleanedChars = input.Where(c => !char.IsPunctuation(c) && !char.IsSymbol(c));
            string withoutPunctuation = new string(cleanedChars.ToArray());

            // Приводим к нижнему регистру
            return withoutPunctuation.ToLowerInvariant();
        }
    }
}