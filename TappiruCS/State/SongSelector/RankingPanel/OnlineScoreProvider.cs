using TappiruCS.GameLogic;
using TappiruCS.Server.Player;

namespace TappiruCS.State.SongSelector.RankingPanel
{
    public class OnlineScoreProvider : IScoreProvider
    {
        public List<PlayerScore> GetScores(string mapHash)
        {
            // TODO: запросить очки с сервера по mapHash и вернуть список
            return new List<PlayerScore>();
        }
    }
}