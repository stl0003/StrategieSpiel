namespace StrategieGameServer.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string NameKey { get; set; } = "UNIT";
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Hp { get; set; } = 100;
        public int MaxHp { get; set; } = 100;
        public string ActiveAction { get; set; } = "move";

        public Unit() { }
        public Unit(int id, int playerId, string nameKey, int x, int y)
        {
            Id = id;
            PlayerId = playerId;
            NameKey = nameKey;
            GridX = x;
            GridY = y;
        }

        public void MoveTo(int newX, int newY)
        {
            GridX = newX;
            GridY = newY;
        }
    }
}