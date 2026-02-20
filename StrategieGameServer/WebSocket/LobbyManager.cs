using System.Collections.Concurrent;
using StrategieGameServer.Models;

namespace StrategieGameServer.WebSocket
{
    public class LobbyManager
    {
        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
        private readonly ConcurrentDictionary<string, string> _connectionToLobby = new();

        public Lobby CreateLobby(string connectionId, string playerName)
        {
            var code = GenerateLobbyCode();
            var lobby = new Lobby(code);
            var player = new Player(connectionId, playerName, lobby.NextPlayerId++);
            lobby.Players.Add(player);

            _lobbies[code] = lobby;
            _connectionToLobby[connectionId] = code;
            return lobby;
        }

        public Lobby? JoinLobby(string connectionId, string lobbyCode, string playerName)
        {
            if (!_lobbies.TryGetValue(lobbyCode, out var lobby)) return null;
            if (lobby.GameStarted) return null;

            var player = new Player(connectionId, playerName, lobby.NextPlayerId++);
            lobby.Players.Add(player);
            _connectionToLobby[connectionId] = lobbyCode;
            return lobby;
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connectionToLobby.TryRemove(connectionId, out var lobbyCode))
            {
                if (_lobbies.TryGetValue(lobbyCode, out var lobby))
                {
                    var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                    if (player != null) lobby.Players.Remove(player);
                    if (lobby.Players.Count == 0) _lobbies.TryRemove(lobbyCode, out _);
                }
            }
        }

        public Lobby? GetLobbyByConnectionId(string connectionId)
        {
            if (_connectionToLobby.TryGetValue(connectionId, out var code))
            {
                _lobbies.TryGetValue(code, out var lobby);
                return lobby;
            }
            return null;
        }

        public List<string> GetConnectionsInLobby(string lobbyCode)
        {
            return _connectionToLobby
                .Where(kv => kv.Value == lobbyCode)
                .Select(kv => kv.Key)
                .ToList();
        }

        public bool StartGame(string lobbyCode)
        {
            if (!_lobbies.TryGetValue(lobbyCode, out var lobby)) return false;
            if (lobby.GameStarted) return false;

            lobby.GameModel = new GameModel();
            var colors = new[] { "RED", "BLUE", "YELLOW", "GREEN" };
            var startPositions = new[]
            {
                new { X = 1, Y = 1 },
                new { X = 1, Y = 28 },
                new { X = 28, Y = 1 },
                new { X = 28, Y = 28 }
            };

            int unitId = 0;
            foreach (var player in lobby.Players)
            {
                if (player.PlayerId < colors.Length)
                {
                    var pos = startPositions[player.PlayerId];
                    var unit = new Unit(unitId++, player.PlayerId, colors[player.PlayerId], pos.X, pos.Y);
                    lobby.GameModel.Units.Add(unit);
                }
            }
            lobby.GameStarted = true;
            return true;
        }

        private string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}