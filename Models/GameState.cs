namespace CommandGame.Models
{
    public class GameState
    {
        public GridTile[][] Grid { get; set; }
        public Ship Ship { get; set; }
        public List<Function> Functions { get; set; }
        public Queue<Command> CommandStack { get; set; }
        public int ExecutionCount { get; set; }
        public int MaxCommands { get; set; } // Maximum number of commands that can be added to a function by the user
        public int MaxExecutions { get; set; } = 1000; // Internal limit for recursive function calls while still preventing infinite loops
        public bool IsGameOver { get; set; }
        public bool IsWin { get; set; }
    }
}
