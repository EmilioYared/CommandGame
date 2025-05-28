namespace CommandGame.Models
{
    public enum TileColor { None, Red, Blue, Green, White }
    public class GridTile
    {
        public TileColor Color { get; set; }
        public bool HasStar { get; set; }
        public bool IsWhite => Color == TileColor.White;
    }
}
