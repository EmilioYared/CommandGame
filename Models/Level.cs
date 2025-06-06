﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int MaxCommands { get; set; } // Maximum number of commands that can be added to a function by the user
        public string TilesJson { get; set; }
        public string ShipStartOrientation { get; set; }
        
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User? Creator { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TileData
    {
        public string Color { get; set; }
        public bool HasStar { get; set; }
    }
}