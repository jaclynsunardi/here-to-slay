namespace HereToSlay.Models;

public enum PendingChoiceKind
{
    SelectCards,
    SelectPlayer,
    SelectOption
}

public enum PendingContinuationType
{
  None,
  QiBearDestroy,
  BearyWisePickFromPool,
  ScryPickOne,
  PickFromDiscard,
  DiscardExactCount,
  PickFromOpponentHand,
  ReturnCursedItem,
  SlipperyPawsDiscardOne,
  OptionalPlayHeroFromHand,
  OptionalPlayItemFromHand,
  OptionalDestroyHero,
  ModifierSign
}

public class PendingContinuation
{
    public PendingContinuationType Type { get; set; }
    public string? TargetPlayerId { get; set; }
    public CardType? DiscardTypeFilter { get; set; }
    public List<string> StagedCardInstanceIds { get; set; } = new();
}

public class PendingChoiceState
{
    public string PlayerId { get; set; } = string.Empty;
    public PendingChoiceKind Kind { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public List<string> SelectableCardInstanceIds { get; set; } = new();
    public List<string> SelectablePlayerIds { get; set; } = new();
    public List<string> Options { get; set; } = new();
    public PendingContinuation Continuation { get; set; } = new();
}
