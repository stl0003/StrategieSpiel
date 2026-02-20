namespace StrategieGameServer.Models
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Type { get; set; } = "PLAINS";
        public bool Explored { get; set; }
        public bool HasTrap { get; set; }

        public Tile() { }
        public Tile(int x, int y, string type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}