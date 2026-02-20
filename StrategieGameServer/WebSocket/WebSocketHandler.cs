using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using StrategieGameServer.Models;

namespace StrategieGameServer.WebSocket
{
    public class GameWebSocketHandler
    {
        private readonly LobbyManager _lobbyManager;
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public GameWebSocketHandler(LobbyManager lobbyManager, WebSocketConnectionManager connectionManager)
        {
            _lobbyManager = lobbyManager;
            _connectionManager = connectionManager;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket webSocket, string connectionId)
        {
            _connectionManager.AddConnection(connectionId, webSocket);
            var buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        _connectionManager.RemoveConnection(connectionId);
                        _lobbyManager.RemoveConnection(connectionId);
                    }
                    else
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessage(webSocket, connectionId, messageJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                _connectionManager.RemoveConnection(connectionId);
                _lobbyManager.RemoveConnection(connectionId);
            }
        }

        private async Task ProcessMessage(System.Net.WebSockets.WebSocket webSocket, string connectionId, string messageJson)
        {
            try
            {
                var baseMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(messageJson, _jsonOptions);
                if (baseMessage == null || !baseMessage.ContainsKey("type"))
                {
                    await SendError(webSocket, "Invalid message format");
                    return;
                }

                var type = baseMessage["type"].ToString();

                switch (type)
                {
                    case "createLobby":
                        var createMsg = JsonSerializer.Deserialize<CreateLobbyMessage>(messageJson, _jsonOptions);
                        if (createMsg != null)
                            await HandleCreateLobby(webSocket, connectionId, createMsg);
                        break;
                    case "joinLobby":
                        var joinMsg = JsonSerializer.Deserialize<JoinLobbyMessage>(messageJson, _jsonOptions);
                        if (joinMsg != null)
                            await HandleJoinLobby(webSocket, connectionId, joinMsg);
                        break;
                    case "startGame":
                        var startMsg = JsonSerializer.Deserialize<StartGameMessage>(messageJson, _jsonOptions);
                        if (startMsg != null)
                            await HandleStartGame(webSocket, connectionId, startMsg);
                        break;
                    case "playerAction":
                        var actionMsg = JsonSerializer.Deserialize<PlayerActionMessage>(messageJson, _jsonOptions);
                        if (actionMsg != null)
                            await HandlePlayerAction(webSocket, connectionId, actionMsg);
                        break;
                    default:
                        await SendError(webSocket, $"Unknown message type: {type}");
                        break;
                }
            }
            catch (JsonException)
            {
                await SendError(webSocket, "Invalid JSON");
            }
        }

        private async Task HandleCreateLobby(System.Net.WebSockets.WebSocket webSocket, string connectionId, CreateLobbyMessage msg)
        {
            var lobby = _lobbyManager.CreateLobby(connectionId, msg.PlayerName);
            var response = new LobbyCreatedMessage
            {
                LobbyCode = lobby.Code,
                Players = lobby.Players,
                YourPlayerId = lobby.Players.First(p => p.ConnectionId == connectionId).PlayerId
            };
            await SendToClient(webSocket, response);
        }

        private async Task HandleJoinLobby(System.Net.WebSockets.WebSocket webSocket, string connectionId, JoinLobbyMessage msg)
        {
            var lobby = _lobbyManager.JoinLobby(connectionId, msg.LobbyCode, msg.PlayerName);
            if (lobby == null)
            {
                await SendError(webSocket, "Lobby not found or already started");
                return;
            }

            var playerJoinedMsg = new PlayerJoinedMessage { Players = lobby.Players };
            await BroadcastToLobby(lobby.Code, playerJoinedMsg);

            var yourPlayerId = lobby.Players.First(p => p.ConnectionId == connectionId).PlayerId;
            await SendToClient(webSocket, new LobbyCreatedMessage
            {
                LobbyCode = lobby.Code,
                Players = lobby.Players,
                YourPlayerId = yourPlayerId
            });
        }

        private async Task HandleStartGame(System.Net.WebSockets.WebSocket webSocket, string connectionId, StartGameMessage msg)
        {
            var lobby = _lobbyManager.GetLobbyByConnectionId(connectionId);
            if (lobby == null || lobby.Code != msg.LobbyCode)
            {
                await SendError(webSocket, "Not authorized to start this lobby");
                return;
            }

            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player?.PlayerId != 0)
            {
                await SendError(webSocket, "Only the lobby creator can start the game");
                return;
            }

            var success = _lobbyManager.StartGame(lobby.Code);
            if (!success)
            {
                await SendError(webSocket, "Could not start game");
                return;
            }

            var gameStartedMsg = new GameStartedMessage { GameModel = lobby.GameModel };
            await BroadcastToLobby(lobby.Code, gameStartedMsg);
        }

        private async Task HandlePlayerAction(System.Net.WebSockets.WebSocket webSocket, string connectionId, PlayerActionMessage msg)
        {
            var lobby = _lobbyManager.GetLobbyByConnectionId(connectionId);
            if (lobby == null || !lobby.GameStarted)
            {
                await SendError(webSocket, "No active game");
                return;
            }

            var unit = lobby.GameModel.Units.FirstOrDefault(u => u.Id == msg.UnitId);
            if (unit == null)
            {
                await SendError(webSocket, "Unit not found");
                return;
            }

            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player?.PlayerId != unit.PlayerId)
            {
                await SendError(webSocket, "Not your unit");
                return;
            }

            bool success = false;
            switch (msg.Action.ToLower())
            {
                case "move":
                    success = lobby.GameModel.MoveUnit(msg.UnitId, msg.TargetX, msg.TargetY);
                    break;
                case "attack":
                    success = lobby.GameModel.AttackUnit(msg.UnitId, msg.TargetX, msg.TargetY);
                    break;
                case "placetrap":
                    success = lobby.GameModel.PlaceTrap(msg.UnitId, msg.TargetX, msg.TargetY);
                    break;
                case "test":
                    Console.WriteLine($"Test action from unit {msg.UnitId} to ({msg.TargetX}, {msg.TargetY})");
                    success = true;
                    break;
                default:
                    await SendError(webSocket, "Unknown action");
                    return;
            }

            if (success)
            {
                var updateMsg = new GameStateUpdatedMessage { GameModel = lobby.GameModel };
                await BroadcastToLobby(lobby.Code, updateMsg);
            }
            else
            {
                await SendError(webSocket, "Action failed");
            }
        }

        private async Task SendToClient(System.Net.WebSockets.WebSocket webSocket, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task BroadcastToLobby(string lobbyCode, WebSocketMessage message)
        {
            var connectionIds = _lobbyManager.GetConnectionsInLobby(lobbyCode);
            var websockets = _connectionManager.GetConnections(connectionIds);
            var json = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            foreach (var ws in websockets)
            {
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        private async Task SendError(System.Net.WebSockets.WebSocket webSocket, string errorMessage)
        {
            var msg = new ErrorMessage { Message = errorMessage };
            await SendToClient(webSocket, msg);
        }
    }
}