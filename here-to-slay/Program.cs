using HereToSlay.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

builder.Services.AddSingleton<Dictionary<string, Game>>();

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();

app.MapPost("/game/create", (CreateGameRequest request, Dictionary<string, Game> games) =>
{
    var host = new Player
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.HostName,
        Type= PlayerType.Host
    };

    var game = new Game
    {
        RoomCode = request.RoomCode,
        Players = new List<Player> {host},
        State = GameState.Waiting
    };

    games[game.RoomCode] = game;

    return Results.Ok(game);
});

app.Run();