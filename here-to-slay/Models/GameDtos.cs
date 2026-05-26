namespace HereToSlay.Models;

public record JoinGameRequest(string PlayerName, string RoomCode);

public record PlayerActionRequest(string PlayerId);

public record PlayCardRequest(
    string PlayerId,
    string CardInstanceId,
    string? TargetHeroInstanceId = null,
    string? TargetPlayerId = null,
    string? ModifierCardInstanceId = null,
    bool RollHeroOnPlay = false);

public record RollHeroRequest(
    string PlayerId,
    string HeroInstanceId,
    string? ModifierCardInstanceId = null);

public record AttackMonsterRequest(
    string PlayerId,
    string MonsterInstanceId,
    string? ModifierCardInstanceId = null);

public record PartyRequirementDto(string? HeroClass, int Count, bool GenericHero = false);

public record CardViewDto(
    string InstanceId,
    string DefinitionId,
    string Name,
    string Type,
    string? HeroClass,
    int RollThreshold,
    int? FailIfRollAtOrBelow,
    string? FailPenalty,
    string ImagePath,
    string EffectText,
    IReadOnlyList<PartyRequirementDto> PartyRequirements,
    IReadOnlyList<CardViewDto> AttachedItems,
    bool IsPartyLeader);

public record PlayerViewDto(
    string Id,
    string Name,
    string Type,
    CardViewDto? PartyLeader,
    IReadOnlyList<CardViewDto> Hand,
    IReadOnlyList<CardViewDto> Party,
    IReadOnlyList<CardViewDto> SlainMonsters,
    int HandCount,
    IReadOnlyList<string> HeroesRolledThisTurn);

public record GameViewDto(
    string RoomCode,
    GameState State,
    string GameType,
    int MaxPlayers,
    int CurrentPlayerIndex,
    int ActionPointsRemaining,
    string? WinnerId,
    string? WinnerName,
    string? LastMessage,
    int? LastRollDie1,
    int? LastRollDie2,
    int? LastRollModifier,
    int? LastRollTotal,
    int DeckCount,
    int MonsterDeckCount,
    int DiscardCount,
    IReadOnlyList<CardViewDto> MonsterRow,
    IReadOnlyList<PlayerViewDto> Players,
    string ViewingPlayerId);
