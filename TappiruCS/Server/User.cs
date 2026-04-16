using System.Net.Http.Json;
using TappiruCS.Server.Player;

namespace TappiruCS.Server
{
    public static class User
    {
        public static async Task<bool> FetchCurrentUser()
        {
            if (string.IsNullOrEmpty(Auth.AuthToken)) return false;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Auth.AuthToken);

                var response = await client.GetAsync("https://tappiruserver.onrender.com/api/user/me");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<UserData>();
                    if (data != null)
                    {
                        PlayerProfile.Instance.UpdateFromServer(data);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchCurrentUser error: {ex.Message}");
            }
            return false;
        }

        // DTO — только для приёма JSON от сервера
        public class UserData
        {
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public int Rating { get; set; }
            public string AvatarPath { get; set; } = "";     // ← используем AvatarPath
            public int PlayCount { get; set; }
            public int AllTimeChar { get; set; }
            public DateTime RegistrationDate { get; set; }
        }
    } 
}

