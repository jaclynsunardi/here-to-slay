using System.Text.Json;
using System.Text.Json.Serialization;
using HereToSlay.Models;
using HereToSlay.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddOpenApi();
var corsOrigins = new List<string> { "http://localhost:5173" };
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    corsOrigins.AddRange(
        frontendUrl.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<Dictionary<string, Game>>();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

app.UseCors();

app.MapPost("/game/create", (CreateGameRequest request, Dictionary<string, Game> games) =>
{
    var roomCode = request.RoomCode.Trim().ToUpperInvariant();
    if (string.IsNullOrWhiteSpace(roomCode))
        return Results.BadRequest(new { message = "Room code is required." });

    if (games.ContainsKey(roomCode))
        return Results.Conflict(new { message = "Room code already exists." });

    var host = new Player
    {
        Id = Guid.NewGuid().ToString(),
        Name = string.IsNullOrWhiteSpace(request.HostName) ? "Host" : request.HostName.Trim(),
        Type = PlayerType.Host
    };

    var game = new Game
    {
        RoomCode = roomCode,
        Players = [host],
        MaxPlayers = Math.Clamp(request.NumPlayers, 2, 6),
        GameType = request.GameType,
        State = GameState.Waiting
    };

    games[roomCode] = game;
    return Results.Ok(new { game.RoomCode, host.Id, host.Name });
});

app.MapPost("/game/join", (JoinGameRequest request, Dictionary<string, Game> games) =>
{
    var roomCode = request.RoomCode.Trim().ToUpperInvariant();
    if (!games.TryGetValue(roomCode, out var game))
        return Results.NotFound(new { message = "Game not found." });

    if (game.State != GameState.Waiting)
        return Results.BadRequest(new { message = "Game already started." });

    if (game.Players.Count >= game.MaxPlayers)
        return Results.BadRequest(new { message = "Lobby is full." });

    var guest = new Player
    {
        Id = Guid.NewGuid().ToString(),
        Name = string.IsNullOrWhiteSpace(request.PlayerName) ? "Guest" : request.PlayerName.Trim(),
        Type = PlayerType.Guest
    };

    game.Players.Add(guest);
    return Results.Ok(new { game.RoomCode, guest.Id, guest.Name });
});

app.MapGet("/game/{roomCode}", (string roomCode, string? playerId, Dictionary<string, Game> games, GameService gameService) =>
{
    roomCode = roomCode.Trim().ToUpperInvariant();
    if (!games.TryGetValue(roomCode, out var game))
        return Results.NotFound(new { message = "Game not found." });

    var viewingId = playerId ?? game.Players.FirstOrDefault()?.Id ?? "";
    return Results.Ok(gameService.ToView(game, viewingId));
});

app.MapPost("/game/{roomCode}/start", (string roomCode, PlayerActionRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g => gameService.StartGame(g)));

app.MapPost("/game/{roomCode}/draw", (string roomCode, PlayerActionRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g => gameService.DrawCard(g, request.PlayerId)));

app.MapPost("/game/{roomCode}/play", (string roomCode, PlayCardRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g =>
        gameService.PlayCard(g, request.PlayerId, request.CardInstanceId, request.TargetHeroInstanceId,
            request.TargetPlayerId, request.ModifierCardInstanceId, request.RollHeroOnPlay)));

app.MapPost("/game/{roomCode}/challenge", (string roomCode, PlayCardRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g =>
    {
        if (string.IsNullOrEmpty(request.TargetPlayerId))
            throw new InvalidOperationException("Target player required.");
        gameService.PlayChallenge(g, request.PlayerId, request.CardInstanceId, request.TargetPlayerId);
    }));

app.MapPost("/game/{roomCode}/roll-hero", (string roomCode, RollHeroRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g =>
        gameService.RollHeroAbility(g, request.PlayerId, request.HeroInstanceId, request.ModifierCardInstanceId)));

app.MapPost("/game/{roomCode}/attack", (string roomCode, AttackMonsterRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g =>
        gameService.AttackMonster(g, request.PlayerId, request.MonsterInstanceId, request.ModifierCardInstanceId)));

app.MapPost("/game/{roomCode}/discard-hand", (string roomCode, PlayerActionRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g => gameService.DiscardHandAndRedraw(g, request.PlayerId)));

app.MapPost("/game/{roomCode}/end-turn", (string roomCode, PlayerActionRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g => gameService.EndTurn(g, request.PlayerId)));

app.MapPost("/game/{roomCode}/resolve-choice", (string roomCode, ResolveChoiceRequest request, Dictionary<string, Game> games, GameService gameService) =>
    HandleAction(roomCode, request.PlayerId, games, gameService, g =>
        gameService.ResolvePendingChoice(g, request.PlayerId, request.SelectedCardInstanceIds, request.SelectedOption)));

app.Run();

static IResult HandleAction(
    string roomCode,
    string playerId,
    Dictionary<string, Game> games,
    GameService gameService,
    Action<Game> action)
{
    roomCode = roomCode.Trim().ToUpperInvariant();
    if (!games.TryGetValue(roomCode, out var game))
        return Results.NotFound(new { message = "Game not found." });

    try
    {
        action(game);
        return Results.Ok(gameService.ToView(game, playerId));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}
