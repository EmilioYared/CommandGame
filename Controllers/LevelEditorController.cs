using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommandGame.Models;
using CommandGame.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CommandGame.Controllers
{
    [Authorize]
    public class LevelEditorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LevelEditorController> _logger;

        public LevelEditorController(ApplicationDbContext context, ILogger<LevelEditorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()  //whenever we put in the url /LevelEditor/Index, this method will be called
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0"); //takes from the jwt token
            var userLevels = _context.Levels
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
            return View(userLevels); //data is passed to Views/LevelEditor/Index.cshtml as a model
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Level level, string tilesJson) //handles the form submission from the Create view
        {
            _logger.LogInformation("Attempting to create level: {Name}", level.Name);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid: {Errors}", 
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return View(level);
            }

            try
            {
                // Validate the tiles JSON
                var tiles = JsonSerializer.Deserialize<List<List<TileData>>>(tilesJson);
                if (tiles == null || tiles.Count != level.Height || tiles[0].Count != level.Width)
                {
                    _logger.LogWarning("Invalid grid data: Height={Height}, Width={Width}, TilesCount={TilesCount}", 
                        level.Height, level.Width, tiles?.Count ?? 0);
                    ModelState.AddModelError("", "Invalid grid data");
                    return View(level);
                }

                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation("Creating level for user {UserId}", userId);

                level.UserId = userId;
                level.TilesJson = tilesJson;
                level.CreatedAt = DateTime.UtcNow;

                _context.Levels.Add(level);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("Level created successfully. SaveChanges result: {Result}", result);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating level: {Message}", ex.Message);
                ModelState.AddModelError("", "An error occurred while creating the level");
                return View(level);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.LevelId == id && l.UserId == userId);
            
            if (level == null)
            {
                return NotFound();
            }

            return View(level);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Level level, string tilesJson)
        {
            if (id != level.LevelId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(level);
            }

            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var existingLevel = await _context.Levels.FirstOrDefaultAsync(l => l.LevelId == id && l.UserId == userId);
                
                if (existingLevel == null)
                {
                    return NotFound();
                }

                // Validate the tiles JSON
                var tiles = JsonSerializer.Deserialize<List<List<TileData>>>(tilesJson);
                if (tiles == null || tiles.Count != level.Height || tiles[0].Count != level.Width)
                {
                    ModelState.AddModelError("", "Invalid grid data");
                    return View(level);
                }

                existingLevel.Name = level.Name;
                existingLevel.Description = level.Description;
                existingLevel.Width = level.Width;
                existingLevel.Height = level.Height;
                existingLevel.ShipStartX = level.ShipStartX;
                existingLevel.ShipStartY = level.ShipStartY;
                existingLevel.MaxCommands = level.MaxCommands;
                existingLevel.ShipStartOrientation = level.ShipStartOrientation;
                existingLevel.TilesJson = tilesJson;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing level");
                ModelState.AddModelError("", "An error occurred while editing the level");
                return View(level);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var level = await _context.Levels.FirstOrDefaultAsync(l => l.LevelId == id && l.UserId == userId);
            
            if (level == null)
            {
                return NotFound();
            }

            _context.Levels.Remove(level);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));  //redirects the user to the Index action after deletion
        }
    }
} 