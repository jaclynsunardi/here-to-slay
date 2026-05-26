namespace HereToSlay.Models;

public enum GameState
{
    Waiting,
    InProgress,
    Finished
}

public class Game
{
    public string RoomCode { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = new();
    public List<CardInstance> Deck { get; set; } = new();
    public List<CardInstance> MonsterDeck { get; set; } = new();
    public List<CardInstance> DiscardPile { get; set; } = new();
    public List<CardInstance> MonsterRow { get; set; } = new();
    public int MaxPlayers { get; set; } = 2;
    public string GameType { get; set; } = "classic";
    public int CurrentPlayerIndex { get; set; }
    public GameState State { get; set; } = GameState.Waiting;
    public int ActionPointsRemaining { get; set; }
    public string? WinnerId { get; set; }
    public string? LastMessage { get; set; }
    public int? LastRollDie1 { get; set; }
    public int? LastRollDie2 { get; set; }
    public int? LastRollModifier { get; set; }
    public int? LastRollTotal { get; set; }
}
