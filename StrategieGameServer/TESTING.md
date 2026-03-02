# Testing Procedures

## Scope
This guide covers baseline checks for:
1. Server startup and health endpoint.
2. Map state loading from `/api/game/state`.
3. Realtime sync polling via `/api/game/mapinfo`.
4. Player actions via `/api/game/action`.

## Prerequisites
- .NET SDK compatible with project target (`net10.0`).
- A modern browser with DevTools (Chrome/Edge).

## Local Smoke Test
1. Restore and build:
   ```powershell
   dotnet restore
   dotnet build
   ```
2. Run the app:
   ```powershell
   dotnet run
   ```
3. Open the app (default route: `/Home/Lobby`).
4. Verify static assets (tiles, JS, CSS) load and startup logs show no blocking errors.

## Health Check (`/health`)
1. Request:
   ```text
   GET /health
   ```
2. Expected:
   - HTTP `200 OK`
   - Body contains `Healthy`

## Game State Check (`/api/game/state`)
1. Request:
   ```text
   GET /api/game/state?lobbyCode=<code>
   ```
2. Expected:
   - HTTP `200 OK`
   - Response object with `tiles`, `units`, `items` (and optional `buildings`)
   - `tiles` is a 30x30 grid
3. Frontend check in DevTools Console:
   ```javascript
   myTiles.length === 30
   myTiles.every(r => r.length === 30)
   ```

## MapInfo Sync Check (`/api/game/mapinfo`)
1. Join or create a lobby so `lobbyCode` is set.
2. In DevTools `Network`, filter for `mapinfo`.
3. Confirm `GET /api/game/mapinfo?lobbyCode=<code>` is called about every 500ms.
4. Confirm responses are `200` and include `units` and `items`.
5. Optional runtime status:
   ```javascript
   getMapInfoSyncStatus()
   ```
   Expected: `enabled: true`, `consecutiveFailures: 0` during stable operation.

## Action Check (`/api/game/action`)
1. In DevTools `Network`, filter for `action`.
2. Move a unit (click action or `W/A/S/D`).
3. Confirm request:
   ```text
   POST /api/game/action?lobbyCode=<code>
   ```
4. Confirm body includes `unitId`, `action`, `targetX`, `targetY` (optional `playerId`).
5. Confirm response is `200` with updated state.

## Regression Checklist
- Unit move/attack/trap actions work without JS errors.
- Item spawn and pickup still sync to the client.
- Lobby + WebSocket flow still starts and runs after page load.
