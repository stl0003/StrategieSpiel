// wwwroot/js/websocket-client.js
let socket = null;
let isMultiplayer = false;
let myPlayerId = null;
let messageHandlers = {}; // Callback-Registrierung

export function connectWebSocket() {
    const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
    const wsUrl = `${protocol}//${window.location.host}/ws`;
    socket = new WebSocket(wsUrl);

    socket.onopen = () => console.log("WebSocket verbunden");
    socket.onmessage = (event) => {
        const data = JSON.parse(event.data);
        if (messageHandlers[data.type]) {
            messageHandlers[data.type](data);
        } else {
            console.warn("Unbekannter Nachrichtentyp:", data.type);
        }
    };
    socket.onclose = () => {
        console.log("WebSocket geschlossen");
        isMultiplayer = false;
        myPlayerId = null;
    };
}

export function registerHandler(type, callback) {
    messageHandlers[type] = callback;
}

export function sendMessage(msg) {
    if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(msg));
    } else {
        console.error("WebSocket nicht verbunden");
    }
}

export function setMultiplayerData(playerId, multiplayerFlag) {
    myPlayerId = playerId;
    isMultiplayer = multiplayerFlag;
}

export function getMyPlayerId() { return myPlayerId; }
export function getIsMultiplayer() { return isMultiplayer; }