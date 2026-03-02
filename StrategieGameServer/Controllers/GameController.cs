using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using StrategieGameServer.Models;

namespace StrategieGameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, GameState> _gameStates = new();

        [HttpGet("state")]
        public ActionResult<GameStateDto> GetGameState([FromQuery] string? lobbyCode)
        {
            var code = lobbyCode ?? "default";
            if (!_gameStates.TryGetValue(code, out var state))
            {
                state = CreateInitialGameState();
                _gameStates[code] = state;
            }
            return Ok(MapToDto(state));
        }

        [HttpPost("action")]
        public ActionResult<GameStateDto> ExecuteAction([FromBody] ActionRequest request, [FromQuery] string? lobbyCode)
        {
            var code = lobbyCode ?? "default";
            if (!_gameStates.TryGetValue(code, out var state))
                return NotFound(new { error = "Lobby not found" });

            var unit = state.Units.FirstOrDefault(u => u.Id == request.UnitId);
            if (unit == null) return BadRequest(new { error = "Unit not found" });
            if (request.PlayerId.HasValue && unit.PlayerId != request.PlayerId.Value)
                return BadRequest(new { error = "Not your unit" });

            try
            {
                switch (request.Action.ToLower())
                {
                    case "move": ExecuteMove(state, unit, request.TargetX, request.TargetY); break;
                    case "attack": ExecuteAttack(state, unit, request.TargetX, request.TargetY); break;
                    case "placetrap": ExecutePlaceTrap(state, unit, request.TargetX, request.TargetY); break;
                    case "test": break;
                    default: return BadRequest(new { error = "Unknown action" });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            return Ok(MapToDto(state));
        }

        [HttpPost("battle/check")]
        public ActionResult<BattleCheckResponse> CheckBattle([FromBody] BattleCheckRequest request, [FromQuery] string? lobbyCode)
        {
            var code = lobbyCode ?? "default";
            if (!_gameStates.TryGetValue(code, out var state))
                return NotFound(new { error = "Lobby not found" });

            var u1 = state.Units.FirstOrDefault(u => u.Id == request.UnitId1);
            var u2 = state.Units.FirstOrDefault(u => u.Id == request.UnitId2);
            if (u1 == null || u2 == null) return BadRequest(new { error = "Unit not found" });

            bool adjacent = (Math.Abs(u1.GridX - u2.GridX) == 1 && u1.GridY == u2.GridY) ||
                            (Math.Abs(u1.GridY - u2.GridY) == 1 && u1.GridX == u2.GridX);
            if (!adjacent) return BadRequest(new { error = "Not adjacent" });

            return Ok(new BattleCheckResponse
            {
                Id = 99,
                Type = "multipleChoice",
                QuestionText = $"⚔️ {u1.NameKey} vs {u2.NameKey}",
                Options = new List<string> { u1.NameKey, u2.NameKey },
                Duration = 30
            });
        }

        private static GameState CreateInitialGameState()
        {
            const int size = 30;
            var rand = new Random();
            var tiles = new Tile[size][];
            for (int y = 0; y < size; y++)
            {
                tiles[y] = new Tile[size];
                for (int x = 0; x < size; x++)
                {
                    double r = rand.NextDouble();
                    string type = r < 0.1 ? "MOUNTAIN" : r < 0.2 ? "WATER" : r < 0.4 ? "FOREST" : "PLAINS";
                    tiles[y][x] = new Tile { Type = type, Explored = false, HasTrap = false };
                }
            }

            var units = new List<Unit>
            {
                new() { Id = 0, PlayerId = 0, NameKey = "RED", GridX = 1, GridY = 1, Hp = 100, MaxHp = 100, Inventory = new() },
                new() { Id = 1, PlayerId = 1, NameKey = "BLUE", GridX = 1, GridY = 28, Hp = 100, MaxHp = 100, Inventory = new() },
                new() { Id = 2, PlayerId = 2, NameKey = "YELLOW", GridX = 28, GridY = 1, Hp = 100, MaxHp = 100, Inventory = new() },
                new() { Id = 3, PlayerId = 3, NameKey = "GREEN", GridX = 28, GridY = 28, Hp = 100, MaxHp = 100, Inventory = new() }
            };

            return new GameState { Tiles = tiles, Units = units, Items = new(), Buildings = new() };
        }

        private static void ExecuteMove(GameState state, Unit unit, int tx, int ty)
        {
            if (tx < 0 || tx >= state.Tiles.Length || ty < 0 || ty >= state.Tiles.Length)
                throw new InvalidOperationException("Out of bounds");
            int dx = Math.Abs(tx - unit.GridX), dy = Math.Abs(ty - unit.GridY);
            if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
                throw new InvalidOperationException("Not adjacent");
            var tile = state.Tiles[ty][tx];
            if (tile.Type == "WATER") throw new InvalidOperationException("Cannot enter water");
            if (state.Units.Any(u => u != unit && u.GridX == tx && u.GridY == ty))
                throw new InvalidOperationException("Tile occupied");

            unit.GridX = tx; unit.GridY = ty;
            if (tile.HasTrap) { unit.Hp -= 20; tile.HasTrap = false; if (unit.Hp <= 0) state.Units.Remove(unit); }
            var item = state.Items.FirstOrDefault(i => i.GridX == tx && i.GridY == ty);
            if (item != null)
            {
                unit.Inventory.Add(item.Type);
                if (item.Type == "HEALTH_PACK") unit.Hp = Math.Min(unit.MaxHp, unit.Hp + 30);
                state.Items.Remove(item);
            }
            for (int y = Math.Max(0, ty - 1); y <= Math.Min(state.Tiles.Length - 1, ty + 1); y++)
                for (int x = Math.Max(0, tx - 1); x <= Math.Min(state.Tiles.Length - 1, tx + 1); x++)
                    state.Tiles[y][x].Explored = true;
        }

        private static void ExecuteAttack(GameState state, Unit attacker, int tx, int ty)
        {
            var target = state.Units.FirstOrDefault(u => u.GridX == tx && u.GridY == ty);
            if (target == null || target == attacker) throw new InvalidOperationException("Invalid target");
            int dx = Math.Abs(tx - attacker.GridX), dy = Math.Abs(ty - attacker.GridY);
            if (dx > 1 || dy > 1 || (dx == 0 && dy == 0)) throw new InvalidOperationException("Not adjacent");
            target.Hp -= 25;
            if (target.Hp <= 0) state.Units.Remove(target);
        }

        private static void ExecutePlaceTrap(GameState state, Unit unit, int tx, int ty)
        {
            if (tx < 0 || tx >= state.Tiles.Length || ty < 0 || ty >= state.Tiles.Length)
                throw new InvalidOperationException("Out of bounds");
            int dx = Math.Abs(tx - unit.GridX), dy = Math.Abs(ty - unit.GridY);
            if (dx > 1 || dy > 1 || (dx == 0 && dy == 0)) throw new InvalidOperationException("Not adjacent");
            if (state.Tiles[ty][tx].HasTrap) throw new InvalidOperationException("Trap already there");
            state.Tiles[ty][tx].HasTrap = true;
        }

        private static GameStateDto MapToDto(GameState state)
        {
            var tiles = new TileDto[state.Tiles.Length][];
            for (int y = 0; y < state.Tiles.Length; y++)
            {
                tiles[y] = new TileDto[state.Tiles[y].Length];
                for (int x = 0; x < state.Tiles[y].Length; x++)
                {
                    var t = state.Tiles[y][x];
                    tiles[y][x] = new TileDto
                    {
                        X = x,
                        Y = y,
                        Type = t.Type,
                        Explored = t.Explored,
                        HasTrap = t.HasTrap
                    };
                }
            }

            return new GameStateDto
            {
                Tiles = tiles,
                Units = state.Units.Select(u => new UnitDto
                {
                    Id = u.Id,
                    PlayerId = u.PlayerId,
                    NameKey = u.NameKey,
                    GridX = u.GridX,
                    GridY = u.GridY,
                    Hp = u.Hp,
                    MaxHp = u.MaxHp,
                    Inventory = u.Inventory
                }).ToList(),
                Items = state.Items.Select(i => new ItemDto
                {
                    GridX = i.GridX,
                    GridY = i.GridY,
                    Type = i.Type
                }).ToList(),
                Buildings = new() // falls du Buildings später brauchst
            };
        }
    }

    public class ActionRequest
    {
        public int UnitId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public int? PlayerId { get; set; }
    }

    public class BattleCheckRequest
    {
        public int UnitId1 { get; set; }
        public int UnitId2 { get; set; }
    }

    public class BattleCheckResponse
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int Duration { get; set; }
    }

    public class GameStateDto
    {
        public TileDto[][] Tiles { get; set; } = Array.Empty<TileDto[]>();
        public List<UnitDto> Units { get; set; } = new();
        public List<ItemDto> Items { get; set; } = new();
        public List<BuildingDto> Buildings { get; set; } = new();
    }

    public class TileDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Explored { get; set; }
        public bool HasTrap { get; set; }
    }

    public class UnitDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string NameKey { get; set; } = string.Empty;
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public List<string> Inventory { get; set; } = new();
    }

    public class ItemDto
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class BuildingDto { }

    public class GameState
    {
        public Tile[][] Tiles { get; set; } = Array.Empty<Tile[]>();
        public List<Unit> Units { get; set; } = new();
        public List<Item> Items { get; set; } = new();
        public List<Building> Buildings { get; set; } = new();
    }

    public class Tile
    {
        public string Type { get; set; } = string.Empty;
        public bool Explored { get; set; }
        public bool HasTrap { get; set; }
    }

    public class Unit
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string NameKey { get; set; } = string.Empty;
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public List<string> Inventory { get; set; } = new();
    }

    public class Item
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class Building { }
}