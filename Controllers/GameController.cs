using Microsoft.AspNetCore.Mvc;
using CommandGame.Services;
using CommandGame.Models;
using CommandGame.Extensions;
using CommandGame.Data;
using System.Text.Json;
using System.Linq;

namespace CommandGame.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CommandListKey = "UserCommands";
        private const string GameStateKey = "GameState";

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(int levelId = 1)
        {
            var userCommands = GetUserCommands();
            var state = HttpContext.Session.GetObject<GameState>(GameStateKey);
            if (state == null)
            {
                state = BuildGameStateFromLevel(levelId);
            }
            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = state.CommandStack?.ToList();
            ViewBag.LevelId = levelId;
            return View("Game", state);
        }

        [HttpPost]
        public IActionResult Index(string commandType, string color, string action, int levelId = 1)
        {
            var userCommands = GetUserCommands();
            if (action == "add")
            {
                userCommands.Add(new UserCommand
                {
                    Type = commandType,
                    Color = color
                });
                SaveUserCommands(userCommands);
                var state = BuildGameStateFromLevel(levelId);
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = state.CommandStack?.ToList();
                ViewBag.LevelId = levelId;
                HttpContext.Session.Remove(GameStateKey);
                return View("Game", state);
            }
            else if (action == "run")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                var state = BuildGameStateFromLevel(levelId);
                state.Functions.Clear();
                state.Functions.Add(function);
                state.CommandStack.Clear();
                foreach (var cmd in function.Commands)
                    state.CommandStack.Enqueue(cmd);
                // Run the game
                var engine = new GameEngine();
                engine.State = state;
                engine.Run();
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = state.CommandStack?.ToList();
                ViewBag.LevelId = levelId;
                HttpContext.Session.SetObject(GameStateKey, state);
                return View("Game", state);
            }
            else if (action == "step")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                var state = HttpContext.Session.GetObject<GameState>(GameStateKey);
                if (state == null)
                {
                    state = BuildGameStateFromLevel(levelId);
                    state.Functions.Clear();
                    state.Functions.Add(function);
                    foreach (var cmd in function.Commands)
                        state.CommandStack.Enqueue(cmd);
                }
                // Step: execute one command
                if (state.CommandStack.Count > 0 && !state.IsGameOver && state.ExecutionCount < state.MaxExecutions)
                {
                    var cmd = state.CommandStack.Dequeue();
                    state.ExecutionCount++;
                    var tile = state.Grid[state.Ship.Y][state.Ship.X];
                    if (cmd.Color == null || cmd.Color == tile.Color)
                    {
                        ExecuteCommand(state, cmd);
                        if (state.Grid[state.Ship.Y][state.Ship.X].IsWhite)
                        {
                            state.IsGameOver = true;
                            state.IsWin = false;
                        }
                        if (state.Grid[state.Ship.Y][state.Ship.X].HasStar)
                        {
                            state.Ship.CollectedStars++;
                            state.Grid[state.Ship.Y][state.Ship.X].HasStar = false;
                        }
                        if (AllStarsCollected(state))
                        {
                            state.IsGameOver = true;
                            state.IsWin = true;
                        }
                    }
                }
                if (!state.IsGameOver && (state.CommandStack.Count == 0 || state.ExecutionCount >= state.MaxExecutions))
                {
                    state.IsGameOver = true;
                    state.IsWin = AllStarsCollected(state);
                }
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = state.CommandStack?.ToList();
                ViewBag.LevelId = levelId;
                HttpContext.Session.SetObject(GameStateKey, state);
                return View("Game", state);
            }
            else if (action == "reset")
            {
                HttpContext.Session.Remove(GameStateKey);
                var state = BuildGameStateFromLevel(levelId);
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = state.CommandStack?.ToList();
                ViewBag.LevelId = levelId;
                return View("Game", state);
            }
            // Default fallback
            var defaultState = BuildGameStateFromLevel(levelId);
            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = defaultState.CommandStack?.ToList();
            ViewBag.LevelId = levelId;
            return View("Game", defaultState);
        }

        private GameState BuildGameStateFromLevel(int levelId)
        {
            var level = _context.Levels.FirstOrDefault(l => l.LevelId == levelId);
            if (level == null) throw new Exception($"Level {levelId} not found");
            var tiles = JsonSerializer.Deserialize<List<List<TileData>>>(level.TilesJson);
            var grid = new GridTile[level.Height][];
            for (int y = 0; y < level.Height; y++)
            {
                grid[y] = new GridTile[level.Width];
                for (int x = 0; x < level.Width; x++)
                {
                    grid[y][x] = new GridTile
                    {
                        Color = Enum.Parse<TileColor>(tiles[y][x].Color),
                        HasStar = tiles[y][x].HasStar
                    };
                }
            }
            return new GameState
            {
                Grid = grid,
                Ship = new Ship { X = level.ShipStartX, Y = level.ShipStartY, CollectedStars = 0 },
                Functions = new List<Function>(),
                CommandStack = new Queue<Command>(),
                ExecutionCount = 0,
                MaxExecutions = level.MaxCommands,
                IsGameOver = false,
                IsWin = false
            };
        }

        private Function BuildFunctionFromUserCommands(List<UserCommand> userCommands)
        {
            return new Function
            {
                Commands = userCommands.Select(cmd => new Command
                {
                    Type = Enum.TryParse<CommandType>(cmd.Type, out CommandType t) ? t : CommandType.Up,
                    Color = cmd.Color == "None" ? null : (Enum.TryParse<TileColor>(cmd.Color, out TileColor c) ? c : (TileColor?)null)
                }).ToList()
            };
        }

        private List<UserCommand> GetUserCommands()
        {
            if (TempData[CommandListKey] is string json)
            {
                TempData.Keep(CommandListKey);
                return JsonSerializer.Deserialize<List<UserCommand>>(json) ?? new List<UserCommand>();
            }
            return new List<UserCommand>();
        }

        private void SaveUserCommands(List<UserCommand> commands)
        {
            TempData[CommandListKey] = JsonSerializer.Serialize(commands);
        }

        // Helpers for step logic
        private void ExecuteCommand(GameState state, Command cmd)
        {
            int width = state.Grid[0].Length;
            int height = state.Grid.Length;
            switch (cmd.Type)
            {
                case CommandType.Up:
                    if (state.Ship.Y > 0) state.Ship.Y--;
                    break;
                case CommandType.Down:
                    if (state.Ship.Y < height - 1) state.Ship.Y++;
                    break;
                case CommandType.Left:
                    if (state.Ship.X > 0) state.Ship.X--;
                    break;
                case CommandType.Right:
                    if (state.Ship.X < width - 1) state.Ship.X++;
                    break;
                case CommandType.ChangeColor:
                    // Not implemented in prototype
                    break;
                case CommandType.CallFunction:
                    if (state.Functions != null && state.Functions.Count > 0)
                    {
                        var functionIndex = cmd.FunctionIndex ?? 0;
                        if (functionIndex >= 0 && functionIndex < state.Functions.Count)
                        {
                            var functionToCall = state.Functions[functionIndex];
                            foreach (var fcmd in functionToCall.Commands)
                                state.CommandStack.Enqueue(fcmd);
                        }
                    }
                    break;
            }
        }
        private bool AllStarsCollected(GameState state)
        {
            foreach (var row in state.Grid)
                foreach (var tile in row)
                    if (tile.HasStar) return false;
            return true;
        }
    }
}
