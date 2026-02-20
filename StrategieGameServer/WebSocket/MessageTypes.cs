using System.Text.Json.Serialization;
using StrategieGameServer.Models;

namespace StrategieGameServer.WebSocket
{
    public abstract class WebSocketMessage
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    // Client → Server
    public class CreateLobbyMessage : WebSocketMessage
    {
        public override string Type => "createLobby";
        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; } = "";
    }

    public class JoinLobbyMessage : WebSocketMessage
    {
        public override string Type => "joinLobby";
        [JsonPropertyName("lobbyCode")]
        public string LobbyCode { get; set; } = "";
        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; } = "";
    }

    public class StartGameMessage : WebSocketMessage
    {
        public override string Type => "startGame";
        [JsonPropertyName("lobbyCode")]
        public string LobbyCode { get; set; } = "";
    }

    public class PlayerActionMessage : WebSocketMessage
    {
        public override string Type => "playerAction";
        [JsonPropertyName("unitId")]
        public int UnitId { get; set; }
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";
        [JsonPropertyName("targetX")]
        public int TargetX { get; set; }
        [JsonPropertyName("targetY")]
        public int TargetY { get; set; }
    }

    // Server → Client
    public class LobbyCreatedMessage : WebSocketMessage
    {
        public override string Type => "lobbyCreated";
        [JsonPropertyName("lobbyCode")]
        public string LobbyCode { get; set; } = "";
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; } = new();
        [JsonPropertyName("yourPlayerId")]
        public int YourPlayerId { get; set; }
    }

    public class PlayerJoinedMessage : WebSocketMessage
    {
        public override string Type => "playerJoined";
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; } = new();
    }

    public class GameStartedMessage : WebSocketMessage
    {
        public override string Type => "gameStarted";
        [JsonPropertyName("gameModel")]
        public GameModel GameModel { get; set; } = null!;
    }

    public class GameStateUpdatedMessage : WebSocketMessage
    {
        public override string Type => "gameStateUpdated";
        [JsonPropertyName("gameModel")]
        public GameModel GameModel { get; set; } = null!;
    }

    public class ErrorMessage : WebSocketMessage
    {
        public override string Type => "error";
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }
}