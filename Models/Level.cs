using System.Collections.Generic;

namespace CommandGame.Models
{
    public class Level
    {
        public int LevelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ShipStartX { get; set; }
        public int ShipStartY { get; set; }
        public int MaxCommands { get; set; }
        public string TilesJson { get; set; }
        public string ShipStartOrientation { get; set; }
    }

    public class TileData
    {
        public string Color { get; set; }
        public bool HasStar { get; set; }
    }
}