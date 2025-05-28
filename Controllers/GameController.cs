using Microsoft.AspNetCore.Mvc;
using CommandGame.Services;
using CommandGame.Models;
using CommandGame.Extensions;
using System.Text.Json;

namespace CommandGame.Controllers
{
    public class GameController : Controller
    {
        private const string CommandListKey = "UserCommands";
        private const string GameStateKey = "GameState";

        [HttpGet]
        public IActionResult Index()
        {
            var userCommands = GetUserCommands();
            var state = HttpContext.Session.GetObject<GameState>(GameStateKey) ?? new GameEngine().State;
            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = state.CommandStack?.ToList();
            return View("Game", state);
        }

        [HttpPost]
        public IActionResult Index(string commandType, string color, string action)
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
                var engine = new GameEngine();
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = engine.State.CommandStack?.ToList();
                HttpContext.Session.Remove(GameStateKey);
                return View("Game", engine.State);
            }
            else if (action == "run")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                var engine = new GameEngine();
                engine.State.Functions.Clear();
                engine.State.Functions.Add(function);
                engine.Run();
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = engine.State.CommandStack?.ToList();
                HttpContext.Session.SetObject(GameStateKey, engine.State);
                return View("Game", engine.State);
            }
            else if (action == "step")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                var state = HttpContext.Session.GetObject<GameState>(GameStateKey);
                if (state == null)
                {
                    var engine = new GameEngine();
                    engine.State.Functions.Clear();
                    engine.State.Functions.Add(function);
                    // Push all commands to stack
                    foreach (var cmd in function.Commands.AsEnumerable().Reverse())
                        engine.State.CommandStack.Push(cmd);
                    state = engine.State;
                }
                // Step: execute one command
                if (state.CommandStack.Count > 0 && !state.IsGameOver && state.ExecutionCount < state.MaxExecutions)
                {
                    var cmd = state.CommandStack.Pop();
                    state.ExecutionCount++;
                    var tile = state.Grid[state.Ship.X, state.Ship.Y];
                    if (cmd.Color == null || cmd.Color == tile.Color)
                    {
                        ExecuteCommand(state, cmd);
                        if (state.Grid[state.Ship.X, state.Ship.Y].IsWhite)
                        {
                            state.IsGameOver = true;
                            state.IsWin = false;
                        }
                        if (state.Grid[state.Ship.X, state.Ship.Y].HasStar)
                        {
                            state.Ship.CollectedStars++;
                            state.Grid[state.Ship.X, state.Ship.Y].HasStar = false;
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
                HttpContext.Session.SetObject(GameStateKey, state);
                return View("Game", state);
            }
            else if (action == "reset")
            {
                HttpContext.Session.Remove(GameStateKey);
                var engine = new GameEngine();
                ViewBag.UserCommands = userCommands;
                ViewBag.CommandStack = engine.State.CommandStack?.ToList();
                return View("Game", engine.State);
            }
            // Default fallback
            var defaultEngine = new GameEngine();
            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = defaultEngine.State.CommandStack?.ToList();
            return View("Game", defaultEngine.State);
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
            switch (cmd.Type)
            {
                case CommandType.Up:
                    if (state.Ship.Y > 0) state.Ship.Y--;
                    break;
                case CommandType.Down:
                    if (state.Ship.Y < state.Grid.GetLength(1) - 1) state.Ship.Y++;
                    break;
                case CommandType.Left:
                    if (state.Ship.X > 0) state.Ship.X--;
                    break;
                case CommandType.Right:
                    if (state.Ship.X < state.Grid.GetLength(0) - 1) state.Ship.X++;
                    break;
                case CommandType.ChangeColor:
                    // Not implemented in prototype
                    break;
                case CommandType.CallFunction:
                    // Not implemented in prototype
                    break;
            }
        }
        private bool AllStarsCollected(GameState state)
        {
            foreach (var tile in state.Grid)
                if (tile.HasStar) return false;
            return true;
        }
    }
} 