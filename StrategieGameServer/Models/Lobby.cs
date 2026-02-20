namespace StrategieGameServer.Models
{
    public class Lobby
    {
        public string Code { get; set; }
        public List<Player> Players { get; set; } = new();
        public GameModel GameModel { get; set; }
        public bool GameStarted { get; set; } = false;
        public int NextPlayerId { get; set; } = 0;

        public Lobby() { }
        public Lobby(string code)
        {
            Code = code;
        }
    }
}