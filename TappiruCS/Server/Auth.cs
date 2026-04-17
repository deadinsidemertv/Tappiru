using System.Net.Http.Json;
using System.IO;

namespace TappiruCS.Server
{
    public static class Auth
    {

        public static string AuthToken { get; set; }

        private static string TokenFilePath = "auth_token.txt";

        public static void SaveToken()
        {
            if (!string.IsNullOrEmpty(AuthToken))
                File.WriteAllText(TokenFilePath, AuthToken);
        }
        public static void LoadToken()
        {
            if (File.Exists(TokenFilePath))
                AuthToken = File.ReadAllText(TokenFilePath);
        }


        public static async Task<bool> Login(string username, string password)
        {
            using var client = new HttpClient();
            var loginData = new { userName = username, password = password };
            var response = await client.PostAsJsonAsync("https://localhost:7068/api/auth/login", loginData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                Auth.AuthToken = result.token;
                SaveToken();    // ← сохраняем токен
                return true;
            }
            return false;
        }
        public class LoginResponse { public string token { get; set; } }

        public static bool IsTokenExpired()
        {
            if (string.IsNullOrEmpty(AuthToken)) return true;

            try
            {
                // Разбираем токен как строку (без проверки)
                var parts = AuthToken.Split('.');
                if (parts.Length != 3) return true;

                // Декодируем вторую часть (payload) из Base64Url
                var payload = parts[1];
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64Url(payload)));

                // Ищем поле "exp" (unix timestamp)
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expElement))
                {
                    var expUnix = expElement.GetInt64();
                    var expireTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    return expireTime < DateTime.UtcNow;
                }
                return true;
            }
            catch
            {
                return true; // при любой ошибке считаем истёкшим
            }
        }

        // Вспомогательный метод: добавляет "=" в конце Base64 строки
        private static string PadBase64Url(string base64)
        {
            int mod = base64.Length % 4;
            if (mod > 0) base64 += new string('=', 4 - mod);
            return base64.Replace('-', '+').Replace('_', '/');
        }
    }
}
