using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace StrategieGameServer.WebSocket
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _connections = new();

        public void AddConnection(string connectionId, System.Net.WebSockets.WebSocket webSocket)
        {
            _connections[connectionId] = webSocket;
        }

        public void RemoveConnection(string connectionId)
        {
            _connections.TryRemove(connectionId, out _);
        }

        public System.Net.WebSockets.WebSocket? GetConnection(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var ws);
            return ws;
        }

        public List<System.Net.WebSockets.WebSocket> GetConnections(IEnumerable<string> connectionIds)
        {
            return connectionIds
                .Select(id => GetConnection(id))
                .Where(ws => ws != null)
                .Select(ws => ws!)
                .ToList();
        }
    }
}