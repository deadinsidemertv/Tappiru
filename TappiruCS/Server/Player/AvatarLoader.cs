using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.Server.Player
{
    public static class AvatarLoader
    {
        private static readonly string BaseUrl = "https://tappiruserver.onrender.com/";

        public static async Task<AvatarLoadResult> LoadAsync(string relativeUrl)
        {
            try
            {
                string fullUrl = BaseUrl.TrimEnd('/') + relativeUrl;

                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(fullUrl);

                var image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);

                return new AvatarLoadResult(image.Data, image.Width, image.Height);
            }
            catch (Exception ex)
            {
                return AvatarLoadResult.Failed(ex.Message);
            }
        }

        public record AvatarLoadResult
        {
            public bool Success { get; }
            public byte[]? Data { get; }
            public int Width { get; }
            public int Height { get; }
            public string? Error { get; }

            public AvatarLoadResult(byte[] data, int w, int h)
            {
                Success = true; Data = data; Width = w; Height = h;
            }
            private AvatarLoadResult(string error) => (Success, Error) = (false, error);
            public static AvatarLoadResult Failed(string msg) => new(msg);
        }
    }
}
