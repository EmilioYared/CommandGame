using CommandGame.Models;

namespace CommandGame.Services
{
    public class GameEngine
    {
        public GameState State { get; set; }

        public GameEngine()
        {
            // Hardcoded 6x6 grid for demo
            int width = 6, height = 6;
            var grid = new GridTile[height][];
            for (int y = 0; y < height; y++)
            {
                grid[y] = new GridTile[width];
                for (int x = 0; x < width; x++)
                {
                    grid[y][x] = new GridTile { Color = TileColor.Green, HasStar = false };
                }
            }

            // Add some colored and white tiles
            grid[0][0].Color = TileColor.Red;
            grid[0][1].Color = TileColor.Blue;
            grid[0][2].Color = TileColor.Green;
            grid[0][3].Color = TileColor.White; // White tile (lose if touched)
            grid[0][4].Color = TileColor.Green;
            grid[0][5].Color = TileColor.Red;
            grid[2][2].HasStar = true;
            grid[4][4].HasStar = true;

            State = new GameState
            {
                Grid = grid,
                Ship = new Ship { X = 1, Y = 0, CollectedStars = 0, Orientation = Orientation.North },
                Functions = new List<Function>(),
                CommandStack = new Queue<Command>(),
                ExecutionCount = 0,
                MaxExecutions = 50
            };

            // Hardcoded function: up, turn right, turn left, callfunction
            var f0 = new Function
            {
                Commands = new List<Command>
                {
                    new Command { Type = CommandType.Up, Color = null },
                    new Command { Type = CommandType.TurnRight, Color = null },
                    new Command { Type = CommandType.TurnLeft, Color = null },
                    new Command { Type = CommandType.CallFunction, Color = null }
                }
            };
            State.Functions.Add(f0);
        }

        public void Run()
        {
            // Enqueue all commands from f0 in original order
            foreach (var cmd in State.Functions[0].Commands)
                State.CommandStack.Enqueue(cmd);

            while (State.CommandStack.Count > 0 && !State.IsGameOver && State.ExecutionCount < State.MaxExecutions)
            {
                var cmd = State.CommandStack.Dequeue();
                State.ExecutionCount++;
                var tile = State.Grid[State.Ship.Y][State.Ship.X];
                // Color rule: only execute if colorless or matches tile
                if (cmd.Color == null || cmd.Color == tile.Color)
                {
                    ExecuteCommand(cmd);
                    // Check for win/lose
                    if (State.Grid[State.Ship.Y][State.Ship.X].IsWhite)
                    {
                        State.IsGameOver = true;
                        State.IsWin = false;
                        break;
                    }
                    if (State.Grid[State.Ship.Y][State.Ship.X].HasStar)
                    {
                        State.Ship.CollectedStars++;
                        State.Grid[State.Ship.Y][State.Ship.X].HasStar = false;
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
            int width = State.Grid[0].Length;
            int height = State.Grid.Length;
            switch (cmd.Type)
            {
                case CommandType.Up:
                    // Move forward in the current orientation
                    switch (State.Ship.Orientation)
                    {
                        case Orientation.North:
                            if (State.Ship.Y > 0) State.Ship.Y--;
                            break;
                        case Orientation.East:
                            if (State.Ship.X < width - 1) State.Ship.X++;
                            break;
                        case Orientation.South:
                            if (State.Ship.Y < height - 1) State.Ship.Y++;
                            break;
                        case Orientation.West:
                            if (State.Ship.X > 0) State.Ship.X--;
                            break;
                    }
                    break;
                case CommandType.TurnLeft:
                    State.Ship.Orientation = (Orientation)(((int)State.Ship.Orientation + 3) % 4);
                    break;
                case CommandType.TurnRight:
                    State.Ship.Orientation = (Orientation)(((int)State.Ship.Orientation + 1) % 4);
                    break;
                case CommandType.CallFunction:
                    if (State.Functions != null && State.Functions.Count > 0)
                    {
                        var functionIndex = cmd.FunctionIndex ?? 0;
                        if (functionIndex >= 0 && functionIndex < State.Functions.Count)
                        {
                            var functionToCall = State.Functions[functionIndex];
                            foreach (var fcmd in functionToCall.Commands)
                                State.CommandStack.Enqueue(fcmd);
                        }
                    }
                    break;
            }
        }

        private bool AllStarsCollected()
        {
            foreach (var row in State.Grid)
                foreach (var tile in row)
                    if (tile.HasStar) return false;
            return true;
        }
    }
} 