using CommandGame.Models;

namespace CommandGame.Services
{
    public class GameEngine
    {
        public GameState State { get; private set; }

        public GameEngine()
        {
            // Hardcoded 6x6 grid for demo
            State = new GameState
            {
                Grid = new GridTile[6, 6],
                Ship = new Ship { X = 1, Y = 0, CollectedStars = 0 },
                Functions = new List<Function>(),
                CommandStack = new Stack<Command>(),
                ExecutionCount = 0,
                MaxExecutions = 50
            };

            // Fill grid with green tiles
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                    State.Grid[x, y] = new GridTile { Color = TileColor.Green, HasStar = false };

            // Add some colored and white tiles
            State.Grid[0, 0].Color = TileColor.Red;
            State.Grid[1, 0].Color = TileColor.Blue;
            State.Grid[2, 0].Color = TileColor.Green;
            State.Grid[3, 0].Color = TileColor.White; // White tile (lose if touched)
            State.Grid[4, 0].Color = TileColor.Green;
            State.Grid[5, 0].Color = TileColor.Red;
            State.Grid[2, 2].HasStar = true;
            State.Grid[4, 4].HasStar = true;

            // Hardcoded function: up, right, right, down
            var f0 = new Function
            {
                Commands = new List<Command>
                {
                    new Command { Type = CommandType.Up, Color = null },
                    new Command { Type = CommandType.Right, Color = TileColor.Blue },
                    new Command { Type = CommandType.Right, Color = null },
                    new Command { Type = CommandType.Down, Color = null }
                }
            };
            State.Functions.Add(f0);
        }

        public void Run()
        {
            // Push all commands from f0 to stack (reverse order for stack)
            foreach (var cmd in State.Functions[0].Commands.Reverse<Command>())
                State.CommandStack.Push(cmd);

            while (State.CommandStack.Count > 0 && !State.IsGameOver && State.ExecutionCount < State.MaxExecutions)
            {
                var cmd = State.CommandStack.Pop();
                State.ExecutionCount++;
                var tile = State.Grid[State.Ship.X, State.Ship.Y];
                // Color rule: only execute if colorless or matches tile
                if (cmd.Color == null || cmd.Color == tile.Color)
                {
                    ExecuteCommand(cmd);
                    // Check for win/lose
                    if (State.Grid[State.Ship.X, State.Ship.Y].IsWhite)
                    {
                        State.IsGameOver = true;
                        State.IsWin = false;
                        break;
                    }
                    if (State.Grid[State.Ship.X, State.Ship.Y].HasStar)
                    {
                        State.Ship.CollectedStars++;
                        State.Grid[State.Ship.X, State.Ship.Y].HasStar = false;
                    }
                    if (AllStarsCollected())
                    {
                        State.IsGameOver = true;
                        State.IsWin = true;
                        break;
                    }
                }
                // else: skip command
            }
            if (!State.IsGameOver)
            {
                State.IsGameOver = true;
                State.IsWin = AllStarsCollected();
            }
        }

        private void ExecuteCommand(Command cmd)
        {
            switch (cmd.Type)
            {
                case CommandType.Up:
                    if (State.Ship.Y > 0) State.Ship.Y--;
                    break;
                case CommandType.Down:
                    if (State.Ship.Y < State.Grid.GetLength(1) - 1) State.Ship.Y++;
                    break;
                case CommandType.Left:
                    if (State.Ship.X > 0) State.Ship.X--;
                    break;
                case CommandType.Right:
                    if (State.Ship.X < State.Grid.GetLength(0) - 1) State.Ship.X++;
                    break;
                case CommandType.ChangeColor:
                    // Not implemented in prototype
                    break;
                case CommandType.CallFunction:
                    // Not implemented in prototype
                    break;
            }
        }

        private bool AllStarsCollected()
        {
            foreach (var tile in State.Grid)
                if (tile.HasStar) return false;
            return true;
        }
    }
} 