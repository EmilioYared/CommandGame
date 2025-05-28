using Microsoft.AspNetCore.Mvc;
using CommandGame.Services;
using CommandGame.Models;
using System.Text.Json;

namespace CommandGame.Controllers
{
    public class GameController : Controller
    {
        private const string CommandListKey = "UserCommands";

        [HttpGet]
        public IActionResult Index()
        {
            // On GET, show empty or existing command list
            var userCommands = GetUserCommands();
            var engine = new GameEngine(); // Default engine (not run)
            ViewBag.UserCommands = userCommands;
            return View("Game", engine.State);
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
                var engine = new GameEngine(); // Not run yet
                ViewBag.UserCommands = userCommands;
                return View("Game", engine.State);
            }
            else if (action == "run")
            {
                // Build function from userCommands
                var function = new Function
                {
                    Commands = userCommands.Select(cmd => new Command
                    {
                        Type = Enum.TryParse<CommandType>(cmd.Type, out CommandType t) ? t : CommandType.Up,
                        Color = cmd.Color == "None" ? null : (Enum.TryParse<TileColor>(cmd.Color, out TileColor c) ? c : (TileColor?)null)
                    }).ToList()
                };
                var engine = new GameEngine();
                engine.State.Functions.Clear();
                engine.State.Functions.Add(function);
                engine.Run();
                ViewBag.UserCommands = userCommands;
                return View("Game", engine.State);
            }
            // Default fallback
            var defaultEngine = new GameEngine();
            ViewBag.UserCommands = userCommands;
            return View("Game", defaultEngine.State);
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
    }
} 