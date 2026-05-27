namespace HereToSlay.Models;

public enum CardType
{
    PartyLeader,
    Hero,
    Item,
    Magic,
    Modifier,
    Challenge,
    Monster
}

public enum HeroClass
{
    Bard,
    Ranger,
    Thief,
    Wizard,
    Guardian,
    Fighter
}

public enum HeroEffect
{
    None,
    Draw1,
    Draw2,
    OpponentDiscard1,
    StealRandomFromHand,
    DestroyOpponentHero,
    SacrificeOwnHeroDraw2,
    SearchDeckDrawHero,
    AllPlayersDiscard1
}

public enum MagicEffect
{
    None,
    Draw2,
    Draw3Discard2,
    DestroyOpponentHero,
    StealOpponentHero,
    AllDraw1,
    ReviveFromDiscard
}

public enum AttackFailPenalty
{
    None,
    SacrificeHero,
    Discard2
}

public enum ItemKind
{
    Equipment,
    Cursed
}

/// <summary>Passive or activated party leader ability (loaded from card data).</summary>
public enum PartyLeaderAbilityKind
{
    None,
    HeroRollBonus,
    OnMagicDraw,
    AttackRollBonus,
    ChallengeRollBonus,
    ModifierChoice,
    StealFromHandOncePerTurn
}

public record PartyRequirement(HeroClass? HeroClass, int Count, bool GenericHero = false);

public class CardDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public HeroClass? HeroClass { get; set; }
    public int RollThreshold { get; set; }
    public int? FightBackMin { get; set; }
    public int? FightBackMax { get; set; }
    public List<PartyRequirement> PartyRequirements { get; set; } = new();
    public HeroEffect HeroEffect { get; set; }
    public MagicEffect MagicEffect { get; set; }
    public ItemKind? ItemKind { get; set; }
    public PartyLeaderAbilityKind PartyLeaderAbility { get; set; }
    public int PartyLeaderAbilityValue { get; set; }
    public int? PartyLeaderAbilityAltValue { get; set; }
    public int ModifierBonus { get; set; }
    /// <summary>Second value for modifiers like +2/-2 (player picks one when playing the card).</summary>
    public int? ModifierBonusAlt { get; set; }
    public int HeroEffectMinRoll { get; set; } = 6;
    public string ImagePath { get; set; } = string.Empty;
    public string EffectText { get; set; } = string.Empty;
    /// <summary>Rules script from JSON (hero/magic effect line). Resolved by CardEffectExecutor.</summary>
    public string EffectScript { get; set; } = string.Empty;
    public int? FailIfRollAtOrBelow { get; set; }
    public AttackFailPenalty FailPenalty { get; set; } = AttackFailPenalty.None;
    public int Copies { get; set; } = 1;
}

public class CardInstance
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public string DefinitionId { get; set; } = string.Empty;
    public List<CardInstance> AttachedItems { get; set; } = new();
}
