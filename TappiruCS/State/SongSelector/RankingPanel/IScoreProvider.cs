using TappiruCS.GameLogic;
using TappiruCS.Server.Player;

namespace TappiruCS.State.SongSelector.RankingPanel
{
    /// <summary>
    /// Источник очков для RankingPanel.
    /// Реализуй этот интерфейс для офлайн и онлайн вариантов.
    /// </summary>
    public interface IScoreProvider
    {
        List<PlayerScore> GetScores(string mapHash);
    }
}