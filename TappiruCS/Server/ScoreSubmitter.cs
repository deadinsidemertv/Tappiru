using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TappiruCS.GameLogic;
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
                    mapName = score.MapName,
                    mapHash = score.MapHash,                    // или название карты, как у тебя хранится
                    score = score._score,
                    accuracy = score._accuraci / 100f,         // если у тебя accuracy в процентах → переводим в 0..1
                    maxCombo = score._maxCobmo,
                    completedChars = score._completeChar,
                    failedChars = score._failChar,
                    completedPhases = score._completePhase,
                    failedPhases = score._failPhase,
                    playedAt = score.PlayedAt
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

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