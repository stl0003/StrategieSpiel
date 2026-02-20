namespace StrategieGameServer.Models
{
    public class Player
    {
        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public bool IsReady { get; set; }
        public int PlayerId { get; set; }

        public Player() { }
        public Player(string connectionId, string name, int playerId)
        {
            ConnectionId = connectionId;
            Name = name;
            PlayerId = playerId;
            IsReady = false;
        }
    }
}