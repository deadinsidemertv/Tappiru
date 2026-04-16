namespace TappiruCS.GameLogic
{
    public class PlayerScore
    {
        public string MapHash { get; set; } = string.Empty;
        public string MapName { get; set; }
        public float _accuraci { get; set; }
        public int _maxCobmo { get; set; }
        public float _score { get; set; }
        public int _completePhase { get; set; }

        public int _failPhase {  get; set; }
        public int _completeChar { get; set; }
        public int _failChar { get; set; }

        public DateTime PlayedAt { get; set; }

        public string PlayerName { get; set; } = "Player";


    }
}
