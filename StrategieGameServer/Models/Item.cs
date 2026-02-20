namespace StrategieGameServer.Models
{
    public class Item
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public string Type { get; set; }

        public Item() { }
        public Item(int x, int y, string type)
        {
            GridX = x;
            GridY = y;
            Type = type;
        }
    }
}