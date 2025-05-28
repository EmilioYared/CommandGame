namespace CommandGame.Models
{
    public class GameState
    {
        public GridTile[][] Grid { get; set; }
        public Ship Ship { get; set; }
        public List<Function> Functions { get; set; }
        public Queue<Command> CommandStack { get; set; }
        public int ExecutionCount { get; set; }
        public int MaxExecutions { get; set; } = 200; // Prevent infinite loops
        public bool IsGameOver { get; set; }
        public bool IsWin { get; set; }
    }
}
