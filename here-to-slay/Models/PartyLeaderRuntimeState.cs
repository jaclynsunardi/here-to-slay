namespace HereToSlay.Models;

/// <summary>Per-player runtime state for party leader abilities (reset each turn).</summary>
public class PartyLeaderRuntimeState
{
    /// <summary>e.g. Shadow Claw hand-steal used this turn.</summary>
    public bool ActiveAbilityUsedThisTurn { get; set; }
}
