using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using TappiruCS.Render;

namespace TappiruCS.Server.Player
{
    public class PlayerProfile
    {
        public static PlayerProfile Instance { get; private set; } = new();
        public bool IsLoggedIn { get; private set; }
        public string UserName { get; private set; } = "";
        public int Rating { get; private set; }
        public string AvatarPath { get; private set; } = "";
        public int PlayCount { get; private set; }
        public int AllTimeChar { get; private set; }
        public DateTime? RegistrationDate { get; private set; }

        public int AvatarTextureId { get; private set; } = 0;

        public event Action OnProfileChanged;

        public void UpdateFromServer(User.UserData data)
        {
            UserName = data.UserName ?? "";
            Rating = data.Rating;
            AvatarPath = data.AvatarPath ?? "";
            PlayCount = data.PlayCount;
            AllTimeChar = data.AllTimeChar;
            RegistrationDate = data.RegistrationDate;

            IsLoggedIn = true;

            // Если аватарка изменилась — перезагружаем
            if (!string.IsNullOrEmpty(AvatarPath))
                _ = LoadAvatarAsync();

            OnProfileChanged?.Invoke();
        }
        private async Task LoadAvatarAsync()
        {
            var result = await AvatarLoader.LoadAsync(AvatarPath);

            if (result.Success && _game != null) 
            {
                _game.InvokeOnMainThread(() =>
                {
                    if (AvatarTextureId != 0)
                        GL.DeleteTexture(AvatarTextureId);

                    AvatarTextureId = TextureLoader.CreateTextureFromRawDataAsync(
                        result.Data, result.Width, result.Height);

                    OnProfileChanged?.Invoke();
                });
            }
        }

        private Game _game;
        public void Initialize(Game game) => _game = game;

        public void Logout()
        {
            IsLoggedIn = false;
            UserName = "";
            AvatarPath = "";
            if (AvatarTextureId != 0)
            {
                GL.DeleteTexture(AvatarTextureId);
                AvatarTextureId = 0;
            }
            OnProfileChanged?.Invoke();
        }
    }
}
