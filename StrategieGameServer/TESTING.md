# Testing Procedures

## Scope
This guide covers:
1. Baseline local validation for the game server and frontend.
2. Tests for map loading from `wwwroot/map.json` in `wwwroot/js/game.js`.
3. Tests for map serialization (`myTiles` -> `generatedMapJsonArray` + console log).

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
3. Open the app (default route is `Home/Lobby`).
4. Verify page assets load (tiles, JS, CSS) and no blocking startup errors appear in terminal.

## Map Loading Test (`/map.json`)
1. Open browser DevTools Console.
2. Reload the page to trigger `onStartGame()`.
3. Confirm this log appears exactly once per page load:
   ```text
   [MAP] Map aus /map.json geladen und als JSON-Array gespeichert:
   ```
4. If `map.json` is missing/invalid, confirm fallback warning appears:
   ```text
   [MAP] Konnte /map.json nicht laden. Fallback auf Zufallsmap.
   ```
5. In fallback case, confirm this log appears:
   ```text
   [MAP] Zufallsmap erstellt und als JSON-Array gespeichert:
   ```

## Map Serialization Test
1. Validate structure in Console:
   ```javascript
   generatedMapJsonArray.length === 30
   generatedMapJsonArray.every(r => r.length === 30)
   generatedMapJsonArray[0][0] // expects: { x, y, type, explored, hasTrap }
   ```
2. Validate values:
   - `x` and `y` match tile coordinates.
   - `type` is one of `PLAINS|FOREST|MOUNTAIN|WATER`.
   - `explored` and `hasTrap` are booleans.

## Where to Find the JSON
- Location: Browser DevTools -> `Console` tab.
- Source 1: Startup log entry:
  ```text
  [MAP] Map aus /map.json geladen und als JSON-Array gespeichert:
  ```
  Expand the logged array object to inspect tile rows and entries.
- Source 2: Runtime variable in Console:
  ```javascript
  generatedMapJsonArray
  ```
- To copy a transport-ready JSON string:
  ```javascript
  copy(JSON.stringify(generatedMapJsonArray))
  ```
- Important: In the current implementation this JSON is kept in memory (JS runtime) and logged to Console; it is not automatically persisted to Local Storage.

## Backend Handoff Readiness Check
Use this in Console to verify payload is serializable for API/WebSocket transfer:
```javascript
const payload = JSON.stringify(generatedMapJsonArray);
payload.length > 0
```
Expected: no serialization errors and non-empty payload string.

## Regression Checks
- Unit movement, trap placement, and item spawn still work after startup.
- Multiplayer start/game update flows still run without JS errors.
