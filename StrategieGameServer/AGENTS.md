# Repository Guidelines

## Project Structure & Module Organization
This is an ASP.NET Core MVC game server (`StrategieGameServer.csproj`, `net10.0`).
- `Controllers/`: HTTP endpoints and page actions (`HomeController`, `LobbyController`, `QuestionsController`).
- `Models/`: game domain objects (`GameModel`, `Unit`, `Tile`, `Lobby`, etc.).
- `WebSocket/`: realtime lobby and socket handling (`WebSocketHandler`, `LobbyManager`).
- `Data/` and `Migrations/`: Entity Framework context and schema history.
- `Views/`: Razor views for UI pages.
- `wwwroot/`: static assets (JS, CSS, images, question files under `wwwroot/fragen/`).

Database work is in progress, but current features mostly persist/read state via JSON files and browser local storage.

## Build, Test, and Development Commands
- `dotnet restore`: restore NuGet packages.
- `dotnet build`: compile the server.
- `dotnet run`: run locally (default route maps to `/Home/Lobby`).
- `dotnet watch run`: run with hot reload for faster iteration.
- `dotnet ef database update`: apply EF migrations when DB-backed changes are needed.

Run commands from the repository root.

## Coding Style & Naming Conventions
- Use 4-space indentation for C#, JS, and Razor.
- C# naming: `PascalCase` for types/methods/properties; `camelCase` for locals/parameters.
- Keep controllers thin; move gameplay/state logic into `Models/`, `Services/`, or `WebSocket/`.
- Prefer descriptive filenames ending with role suffixes (for example, `*Controller.cs`, `*Manager.cs`, `*Handler.cs`).
- No formatter/linter is configured; follow existing style in neighboring files and keep diffs focused.

## Testing Guidelines
There is currently no dedicated test project in this repository.
- Minimum gate before PR: `dotnet build` and manual verification of Lobby + WebSocket flows.
- For new logic-heavy features, add focused unit tests in a future `tests/` project (xUnit recommended) and name files like `GameModelTests.cs`.

## Commit & Pull Request Guidelines
Recent history favors short, imperative commit subjects (for example, `Add inventory panel`, `Bug fix`).
- Commit format: `<Verb> <scope/feature>`; keep subject concise and specific.
- One feature/fix per commit when possible.
- PRs should include: change summary, manual test steps, linked issue (if any), and screenshots/GIFs for UI or gameplay changes.

## Security & Configuration Tips
- Keep environment-specific values in `appsettings.Development.json`; do not commit secrets.
- Validate `ConnectionStrings:DefaultConnection` before running migrations.
- Treat files under `wwwroot/fragen/` as content data; review changes carefully.
