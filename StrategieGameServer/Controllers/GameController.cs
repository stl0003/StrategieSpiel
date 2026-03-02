using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace StrategieGameServer.Controllers
{
    [ApiController]
    [Route("api/game")]
    public class GameController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public GameController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetState()
        {
            var configuredPath = _configuration["GameState:MapFilePath"];
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = Environment.GetEnvironmentVariable("GAME_MAP_JSON_PATH");
            }

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return StatusCode(500, new
                {
                    error = "Map path missing. Configure GameState:MapFilePath or GAME_MAP_JSON_PATH."
                });
            }

            var fullPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { error = $"Map file not found: {fullPath}" });
            }

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(fullPath);
                var tiles = JsonSerializer.Deserialize<JsonElement>(json);

                if (tiles.ValueKind != JsonValueKind.Array)
                {
                    return StatusCode(500, new { error = "Invalid map format. Expected top-level JSON array." });
                }

                return Ok(new { tiles });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { error = "Invalid map JSON.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to load game state.", detail = ex.Message });
            }
        }
    }
}
