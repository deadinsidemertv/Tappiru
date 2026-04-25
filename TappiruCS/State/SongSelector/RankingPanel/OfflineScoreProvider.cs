using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Server.Player;

namespace TappiruCS.State.SongSelector.RankingPanel
{
    /// <summary>
    /// Читает очки из локальной базы (ScoreManager).
    /// Работает без подключения к серверу.
    /// </summary>
    public class OfflineScoreProvider : IScoreProvider
    {
        private const int MaxScores = 10;

        public List<PlayerScore> GetScores(string mapHash)
        {
            return ScoreManager.GetTopScoresForMap(mapHash, MaxScores);
        }
    }
}