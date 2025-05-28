namespace CommandGame.Models
{
    public enum CommandType { Up, Down, Left, Right, ChangeColor, CallFunction }
    public class Command
    {
        public CommandType Type { get; set; }
        public TileColor? Color { get; set; } // null = colorless
        public int? FunctionIndex { get; set; } // for CallFunction
    }
}
