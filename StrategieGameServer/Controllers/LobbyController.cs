using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using StrategieGameServer.Models;       // Enthält die Lobby-Klasse

namespace MyGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        // Hier greifen wir auf die gleiche Lobby-Liste zu, die auch der WebSocket-Handler verwendet.
        // In einer echten Anwendung würdest du einen Service (z.B. ILobbyManager) per Dependency Injection nutzen.
        private static readonly Dictionary<string, Lobby> Lobbies = new();

        [HttpGet]
        public IActionResult GetLobbies()
        {
            var lobbyInfos = Lobbies.Values.Select(l => new
            {
                l.Code,
                PlayerCount = l.Players.Count,
                l.GameStarted
            });
            return Ok(lobbyInfos);
        }

        [HttpGet("{code}")]
        public IActionResult GetLobby(string code)
        {
            if (Lobbies.TryGetValue(code, out var lobby))
            {
                return Ok(lobby);
            }
            return NotFound();
        }
    }
}