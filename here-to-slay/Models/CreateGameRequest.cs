namespace HereToSlay.Models;

public record CreateGameRequest(string HostName, string RoomCode, int NumPlayers, string GameType);