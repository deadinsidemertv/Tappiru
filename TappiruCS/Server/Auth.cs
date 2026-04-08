using System.Net.Http.Json;

namespace TappiruCS.Server
{
    public static class Auth
    {

        public static string AuthToken { get; set; }

        public static async Task<bool> Login(string username, string password)
        {
            using var client = new HttpClient();
            var loginData = new { userName = username, password = password };
            var response = await client.PostAsJsonAsync("https://localhost:7068/api/auth/login", loginData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                Auth.AuthToken = result.token;
                return true;
            }
            return false;
        }
        public class LoginResponse { public string token { get; set; } }
    }
}
