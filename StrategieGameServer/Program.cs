using StrategieGameServer.WebSocket;
using StrategieGameServer.Models;
using Microsoft.EntityFrameworkCore;
using StrategieGameServer.Data;   // für GameDbContext
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LobbyManager>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddScoped<GameWebSocketHandler>();

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var connectionId = Guid.NewGuid().ToString();
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<GameWebSocketHandler>();
        await handler.HandleConnectionAsync(webSocket, connectionId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();