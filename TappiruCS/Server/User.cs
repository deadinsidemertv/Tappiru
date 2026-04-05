using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

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
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Auth.AuthToken);
            var response = await client.GetAsync("https://localhost:7068/api/user/me");
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
                    return true;
                }
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

