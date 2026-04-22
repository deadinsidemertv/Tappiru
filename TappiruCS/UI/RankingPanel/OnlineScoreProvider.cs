using TappiruCS.GameLogic;
using TappiruCS.Server.Player;
using TappiruCS.UI.RankingPanel;

namespace TappiruCS.UI.RankingPanel
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