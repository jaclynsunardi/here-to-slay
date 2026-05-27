namespace HereToSlay.Models;

/// <summary>Temporary buffs from hero abilities until end of turn.</summary>
public class HeroTurnBuffs
{
    public int RollBonusUntilEndOfTurn { get; set; }
    public bool HeroesCannotBeStolen { get; set; }
    public bool HeroesCannotBeDestroyed { get; set; }
    public bool CardsCannotBeChallenged { get; set; }
}
