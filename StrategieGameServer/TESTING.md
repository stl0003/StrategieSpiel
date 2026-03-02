# Testing Procedures

## Scope
This guide covers:
1. Baseline local validation for the game server and frontend.
2. Tests for map loading from backend endpoint `/api/game/state` in `wwwroot/js/game.js`.
3. Validation that the loaded JSON is applied to runtime tiles (`myTiles`).

## Prerequisites
- .NET SDK compatible with project target (`net10.0`).
- A modern browser with DevTools (Chrome/Edge).
- Backend map source configured for `GET /api/game/state`:
  - `GameState:MapFilePath`, or
  - environment variable `GAME_MAP_JSON_PATH`.

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

## Map Loading Test (`/api/game/state`)
1. Open browser DevTools Console.
2. Reload the page to trigger `onStartGame()`.
3. Confirm this log appears exactly once per page load:
   ```text
   [MAP] Map vom Backend (/api/game/state) geladen.
   ```
4. If backend map is missing/invalid, confirm error appears:
   ```text
   [MAP] Konnte Map nicht vom Backend laden (/api/game/state). Frontend-Map-Start abgebrochen.
   ```
5. In this error case, no map should be generated on the frontend.

## Applied Map Validation
1. Validate structure in Console:
   ```javascript
   myTiles.length === 30
   myTiles.every(r => r.length === 30)
   myTiles[0][0] // expects tile object with .type / .explored / .hasTrap
   ```
2. Validate values:
   - `myTiles[0][0].type` is one of `PLAINS|FOREST|MOUNTAIN|WATER`.
   - `myTiles[0][0].explored` and `myTiles[0][0].hasTrap` are booleans.

## Where to Find the JSON
- Location: Browser DevTools -> `Console` tab.
- Source 1: Startup log entry:
  ```text
  [MAP] Map vom Backend (/api/game/state) geladen.
  ```
  Request payload is fetched from backend and applied into `myTiles`.
- Source 2: Inspect loaded runtime tiles:
  ```javascript
  myTiles
  ```

## Backend Endpoint Check
Use this endpoint directly to validate backend delivery:
```text
GET /api/game/state
```
Expected: HTTP 200 and either:
- a 30x30 tile array directly, or
- a state object containing `tiles` (or `gameModel.tiles` / `gameState.tiles`).

## Regression Checks
- Unit movement, trap placement, and item spawn still work after startup.
- Multiplayer start/game update flows still run without JS errors.
- Frontend does not generate random terrain anymore; map source is backend (`/api/game/state`) only.
