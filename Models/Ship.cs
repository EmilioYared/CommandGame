namespace CommandGame.Models
{
    public enum Orientation { North, East, South, West }

    public class Ship
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int CollectedStars { get; set; }
        public Orientation Orientation { get; set; }
    }
}
