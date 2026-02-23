namespace StrategieGameServer.Models
{
    public class Building
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public string Type { get; set; }
        public int OwnerPlayerId { get; set; }

        public Building() { }
        public Building(int x, int y, string type, int ownerId)
        {
            GridX = x;
            GridY = y;
            Type = type;
            OwnerPlayerId = ownerId;
        }
    }
}

//Hallo leif 