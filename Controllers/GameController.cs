using Microsoft.AspNetCore.Mvc;
using CommandGame.Services;
using CommandGame.Models;
using CommandGame.Extensions;
using CommandGame.Data;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CommandGame.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GameController> _logger;
        private const string CommandListKey = "UserCommands";
        private const string GameStateKey = "GameState";
        private const int MaxExecutionsLimit = 1000; // Internal limit to prevent infinite loops during execution

        public GameController(ApplicationDbContext context, ILogger<GameController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(int levelId = 1)
        {
            HttpContext.Session.Remove(GameStateKey);
            var userCommands = GetUserCommands();
            var state = BuildGameStateFromLevel(levelId);
            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = state.CommandStack?.ToList();
            ViewBag.LevelId = levelId;
            return View("Game", state);
        }

        [HttpPost]
        public IActionResult Index(string commandType, string color, string action, int levelId = 1)
        {
            var userCommands = GetUserCommands();
            GameState state;

            if (action == "add")
            {
                var level = _context.Levels.FirstOrDefault(l => l.LevelId == levelId);
                if (level == null) throw new Exception($"Level {levelId} not found");
                
                if (userCommands.Count >= level.MaxCommands)
                {
                    _logger.LogWarning("Attempted to add command when limit reached: {Count}/{Max}", userCommands.Count, level.MaxCommands);
                    TempData["Error"] = $"Cannot add more commands. Maximum limit is {level.MaxCommands}.";
                }
                else
                {
                    userCommands.Add(new UserCommand
                    {
                        Type = commandType,
                        Color = color
                    });
                    SaveUserCommands(userCommands);
                }
                state = BuildGameStateFromLevel(levelId);
                HttpContext.Session.Remove(GameStateKey);
            }
            else if (action == "clear")
            {
                userCommands.Clear();
                SaveUserCommands(userCommands);
                state = BuildGameStateFromLevel(levelId);
                HttpContext.Session.Remove(GameStateKey);
            }
            else if (action == "run")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                state = BuildGameStateFromLevel(levelId);
                state.Functions.Clear();
                state.Functions.Add(function);
                state.CommandStack.Clear();
                foreach (var cmd in function.Commands)
                    state.CommandStack.Enqueue(cmd);
                // Run the game
                var engine = new GameEngine();
                engine.State = state;
                engine.Run();
                HttpContext.Session.SetObject(GameStateKey, state);
            }
            else if (action == "step")
            {
                var function = BuildFunctionFromUserCommands(userCommands);
                state = HttpContext.Session.GetObject<GameState>(GameStateKey);
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
                HttpContext.Session.SetObject(GameStateKey, state);
            }
            else if (action == "reset")
            {
                HttpContext.Session.Remove(GameStateKey);
                state = BuildGameStateFromLevel(levelId);
            }
            else
            {
                state = BuildGameStateFromLevel(levelId);
            }

            ViewBag.UserCommands = userCommands;
            ViewBag.CommandStack = state.CommandStack?.ToList();
            ViewBag.LevelId = levelId;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_GameContent", state);
            }
            return View("Game", state);
        }

        [HttpGet]
        public IActionResult SelectLevel()
        {
            var levels = _context.Levels
                .OrderBy(l => l.LevelId)
                .Select(l => new LevelViewModel
                {
                    LevelId = l.LevelId,
                    Name = l.Name,
                    Description = l.Description,
                    MaxCommands = l.MaxCommands,
                    CreatedAt = l.CreatedAt,
                    CreatorName = l.Creator != null ? l.Creator.Username : "System"
                })
                .ToList();
            
            return View(levels);
        }

        private GameState BuildGameStateFromLevel(int levelId)
        {
            var level = _context.Levels.FirstOrDefault(l => l.LevelId == levelId);
            if (level == null) throw new Exception($"Level {levelId} not found");
            
            _logger.LogInformation("Building game state for level {LevelId}: {Name}", levelId, level.Name);
            _logger.LogInformation("Raw TilesJson: {TilesJson}", level.TilesJson);
            
            if (string.IsNullOrEmpty(level.TilesJson))
            {
                _logger.LogError("TilesJson is null or empty for level {LevelId}", levelId);
                throw new Exception("Level has no tile data");
            }
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var tiles = JsonSerializer.Deserialize<List<List<TileData>>>(level.TilesJson, options);
                if (tiles == null)
                {
                    _logger.LogError("Failed to deserialize tiles JSON");
                    throw new Exception("Invalid tiles data");
                }
                
                _logger.LogInformation("Deserialized tiles: {Tiles}", JsonSerializer.Serialize(tiles));
                _logger.LogInformation("Grid dimensions: Height={Height}, Width={Width}, Tiles rows={Rows}, First row columns={Columns}", 
                    level.Height, level.Width, tiles.Count, tiles.FirstOrDefault()?.Count ?? 0);
                
                if (tiles.Count != level.Height)
                {
                    _logger.LogError("Tile row count ({Rows}) does not match level height ({Height})", 
                        tiles.Count, level.Height);
                    throw new Exception("Invalid grid dimensions");
                }
                
                if (tiles.FirstOrDefault()?.Count != level.Width)
                {
                    _logger.LogError("Tile column count ({Columns}) does not match level width ({Width})", 
                        tiles.FirstOrDefault()?.Count ?? 0, level.Width);
                    throw new Exception("Invalid grid dimensions");
                }
                
                var grid = new GridTile[level.Height][];
                for (int y = 0; y < level.Height; y++)
                {
                    if (tiles[y] == null)
                    {
                        _logger.LogError("Row {Y} is null", y);
                        throw new Exception($"Invalid tile data at row {y}");
                    }
                    
                    grid[y] = new GridTile[level.Width];
                    for (int x = 0; x < level.Width; x++)
                    {
                        if (tiles[y][x] == null)
                        {
                            _logger.LogError("Tile at ({X}, {Y}) is null", x, y);
                            throw new Exception($"Invalid tile data at ({x}, {y})");
                        }
                        
                        if (string.IsNullOrEmpty(tiles[y][x].Color))
                        {
                            _logger.LogError("Tile color at ({X}, {Y}) is null or empty", x, y);
                            throw new Exception($"Invalid tile color at ({x}, {y})");
                        }
                        
                        _logger.LogInformation("Processing tile at ({X}, {Y}): Color={Color}, HasStar={HasStar}", 
                            x, y, tiles[y][x].Color, tiles[y][x].HasStar);
                        
                        try 
                        {
                            // Try to parse the color, handling both formats (with and without 'Color' suffix)
                            string colorStr = tiles[y][x].Color;
                            _logger.LogInformation("Raw color string: {ColorStr}", colorStr);
                            
                            // Remove any 'Color' suffix if present
                            if (colorStr.EndsWith("Color"))
                            {
                                colorStr = colorStr.Substring(0, colorStr.Length - 5);
                                _logger.LogInformation("Removed 'Color' suffix: {ColorStr}", colorStr);
                            }
                            
                            _logger.LogInformation("Attempting to parse color: {ColorStr}", colorStr);
                            
                            // Try parsing with case-insensitive comparison
                            if (!Enum.TryParse<TileColor>(colorStr, true, out var parsedColor))
                            {
                                _logger.LogError("Failed to parse color: {ColorStr}. Valid values are: {ValidValues}", 
                                    colorStr, string.Join(", ", Enum.GetNames(typeof(TileColor))));
                                throw new Exception($"Invalid tile color value: {colorStr}. Must be one of: {string.Join(", ", Enum.GetNames(typeof(TileColor)))}");
                            }
                            
                            _logger.LogInformation("Successfully parsed color: {ParsedColor}", parsedColor);
                            
                            grid[y][x] = new GridTile
                            {
                                Color = parsedColor,
                                HasStar = tiles[y][x].HasStar
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse tile color at ({X}, {Y}): Raw color={RawColor}", 
                                x, y, tiles[y][x].Color);
                            throw;
                        }
                    }
                }
                
                Orientation orientation;
                if (string.IsNullOrEmpty(level.ShipStartOrientation))
                {
                    _logger.LogWarning("ShipStartOrientation is null or empty, defaulting to North");
                    orientation = Orientation.North;
                }
                else if (!Enum.TryParse<Orientation>(level.ShipStartOrientation, out orientation))
                {
                    _logger.LogWarning("Failed to parse ShipStartOrientation: {Orientation}, defaulting to North", level.ShipStartOrientation);
                    orientation = Orientation.North;
                }
                else
                {
                    _logger.LogInformation("Successfully parsed ShipStartOrientation: {Orientation}", orientation);
                }
                
                return new GameState
                {
                    Grid = grid,
                    Ship = new Ship { X = level.ShipStartX, Y = level.ShipStartY, CollectedStars = 0, Orientation = orientation },
                    Functions = new List<Function>(),
                    CommandStack = new Queue<Command>(),
                    ExecutionCount = 0,
                    MaxCommands = level.MaxCommands, // User-visible limit for adding commands
                    MaxExecutions = MaxExecutionsLimit, // Internal limit for execution
                    IsGameOver = false,
                    IsWin = false
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse tiles JSON: {Json}", level.TilesJson);
                throw new Exception("Invalid tiles JSON format", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building game state for level {LevelId}", levelId);
                throw;
            }
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
                    // Move forward in the current orientation
                    switch (state.Ship.Orientation)
                    {
                        case Orientation.North:
                            if (state.Ship.Y > 0) state.Ship.Y--;
                            break;
                        case Orientation.East:
                            if (state.Ship.X < width - 1) state.Ship.X++;
                            break;
                        case Orientation.South:
                            if (state.Ship.Y < height - 1) state.Ship.Y++;
                            break;
                        case Orientation.West:
                            if (state.Ship.X > 0) state.Ship.X--;
                            break;
                    }
                    break;
                case CommandType.TurnLeft:
                    state.Ship.Orientation = (Orientation)(((int)state.Ship.Orientation + 3) % 4);
                    break;
                case CommandType.TurnRight:
                    state.Ship.Orientation = (Orientation)(((int)state.Ship.Orientation + 1) % 4);
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

    public class LevelViewModel
    {
        public int LevelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxCommands { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; }
    }
}
