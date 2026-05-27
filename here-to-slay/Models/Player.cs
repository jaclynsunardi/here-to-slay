namespace HereToSlay.Models;

public enum PlayerType
{
    Host,
    Guest
}

public class Player
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PlayerType Type { get; set; } = PlayerType.Guest;
    public CardInstance? PartyLeader { get; set; }
    public PartyLeaderRuntimeState PartyLeaderRuntime { get; set; } = new();
    public HeroTurnBuffs HeroTurnBuffs { get; set; } = new();
    public List<CardInstance> Hand { get; set; } = new();
    public List<CardInstance> Party { get; set; } = new();
    public List<CardInstance> SlainMonsters { get; set; } = new();
    public HashSet<string> HeroesRolledThisTurn { get; set; } = new();
    public string? ChallengedByPlayerId { get; set; }
    public string? ActiveChallengeCardInstanceId { get; set; }
}
