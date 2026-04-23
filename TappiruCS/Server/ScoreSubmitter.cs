using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Mod;
using TappiruCS.Server.Player;

namespace TappiruCS.Server
{
    public static class ScoreSubmitter
    {
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7068/")   
        };

        public static async Task<bool> SubmitScoreAsync(PlayerScore score)
        {

            if (string.IsNullOrEmpty(Auth.AuthToken))
            {
                Console.WriteLine("Нет токена — результат не отправлен (оффлайн)");
                return false;
            }

            try
            {
                var dto = new
                {
                    mapName = score.MapName, // или название карты, как у тебя хранится
                    mapHash = score.MapHash,

                    score = score._score,
                    accuracy = score._accuraci,         // если у тебя accuracy в процентах → переводим в 0..1
                    maxCombo = score._maxCobmo,

                    completedChars = score._completeChar,
                    failedChars = score._failChar,
                    completedPhases = score._completePhase,
                    failedPhases = score._failPhase,

                    playedAt = score.PlayedAt,

                    perfectSlider = score._perfectSlider,
                    goodSlider  = score._goodSlider,

                    mods = score.mods
                };
                Console.WriteLine($"[DEBUG] DTO before save: perfectSlider={dto.perfectSlider}, goodSlider={dto.goodSlider}");
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("JSON SENT: " + json);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Auth.AuthToken);

                var response = await client.PostAsync("/api/scores/submit", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Результат успешно отправлен на сервер!");

                    // После успешной отправки обновляем профиль игрока
                    await User.FetchCurrentUser();

                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка отправки скора: {response.StatusCode} - {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке результата: {ex.Message}");
                return false;
            }
        }
    }
}