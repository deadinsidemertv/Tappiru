using System.Net.Http.Json;

namespace TappiruCS.Server
{
    public static class User
    {
        public static bool IsOnline { get; set; } = false;
        public static string UserName { get; set; }
        public static string Email { get; set; }
        public static int Rating { get; set; }
        public static string AvatarPath { get; set; } // может быть URL или локальный путь
        public static int PlayCount { get; set; }
        public static int AllTimeChar { get; set; }
        public static DateTime? RegistrationDate { get; set; }
        public static async Task<bool> FetchCurrentUser()
        {
            if (string.IsNullOrEmpty(Auth.AuthToken))
            {
                Console.WriteLine("FetchCurrentUser: AuthToken пустой!");
                return false;
            }

            try
            {
                using var client = new HttpClient();

                // Явно добавляем заголовок (иногда DefaultRequestHeaders ведёт себя странно)
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Auth.AuthToken);

                Console.WriteLine($"Отправляем запрос на /api/user/me с токеном длиной {Auth.AuthToken.Length}");

                var response = await client.GetAsync("https://localhost:7068/api/user/me");

                Console.WriteLine($"Статус ответа: {(int)response.StatusCode} {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<UserData>();
                    if (data != null)
                    {
                        UserName = data.UserName;
                        Email = data.Email;
                        Rating = data.Rating;
                        AvatarPath = data.AvatarPath;
                        PlayCount = data.PlayCount;
                        AllTimeChar = data.AllTimeChar;
                        RegistrationDate = data.RegistrationDate;
                        IsOnline = true;

                        Console.WriteLine($"Успешно получены данные пользователя: {UserName}, рейтинг {Rating}");
                        return true;
                    }
                }
                else
                {
                    // ← Это самое важное — смотрим, что именно ответил сервер
                    var errorText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ОШИБКА сервера: {errorText}");
                    Console.WriteLine($"Полный статус: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение в FetchCurrentUser: {ex.GetType().Name} - {ex.Message}");
            }

            return false;
        }

        private class UserData
        {
            public string UserName { get; set; }
            public string Email { get; set; }
            public int Rating { get; set; }
            public string AvatarPath { get; set; }
            public int PlayCount { get; set; }
            public int AllTimeChar { get; set; }
            public DateTime RegistrationDate { get; set; }
        }
    } 
}

