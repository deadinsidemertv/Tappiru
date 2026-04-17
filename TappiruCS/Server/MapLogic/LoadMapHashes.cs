using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TappiruCS.Server.MapLogic
{
    public static class LoadMapHashes
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static HashSet<string> _serverMapHashes = new HashSet<string>();
        private static bool _isLoaded = false;
        private static readonly object _lock = new object();

        public static async Task<HashSet<string>> GetServerMapHashesAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _isLoaded)
                return _serverMapHashes;

            lock (_lock)
            {
                if (!forceRefresh && _isLoaded)
                    return _serverMapHashes;
            }

            try
            {
                // Замените URL на ваш реальный эндпоинт
                var response = await _httpClient.GetAsync("https://localhost:7068/api/maps/hashes");
                response.EnsureSuccessStatusCode();

                var hashes = await response.Content.ReadFromJsonAsync<List<string>>();
                if (hashes != null)
                {
                    lock (_lock)
                    {
                        _serverMapHashes = new HashSet<string>(hashes);
                        _isLoaded = true;
                    }
                    Console.WriteLine($"[ServerMapCache] Загружено {hashes.Count} хэшей карт");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerMapCache] Ошибка загрузки хэшей: {ex.Message}");
                // Возвращаем уже загруженные (если есть) или пустой HashSet
            }

            return _serverMapHashes;
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _serverMapHashes.Clear();
                _isLoaded = false;
            }
        }
    }
}
