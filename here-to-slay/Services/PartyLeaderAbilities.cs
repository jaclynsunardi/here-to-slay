using HereToSlay.Models;

namespace HereToSlay.Services;

public static class PartyLeaderAbilities
{
    public static CardDefinition? GetDefinition(Player player) =>
        player.PartyLeader == null ? null : CardCatalog.Get(player.PartyLeader.DefinitionId);

    public static PartyLeaderAbilityKind GetKind(Player player) =>
        GetDefinition(player)?.PartyLeaderAbility ?? PartyLeaderAbilityKind.None;

    public static int HeroRollBonus(Player player) =>
        GetKind(player) == PartyLeaderAbilityKind.HeroRollBonus
            ? GetDefinition(player)!.PartyLeaderAbilityValue
            : 0;

    public static int AttackRollBonus(Player player) =>
        GetKind(player) == PartyLeaderAbilityKind.AttackRollBonus
            ? GetDefinition(player)!.PartyLeaderAbilityValue
            : 0;

    public static int ChallengeRollBonus(Player player) =>
        GetKind(player) == PartyLeaderAbilityKind.ChallengeRollBonus
            ? GetDefinition(player)!.PartyLeaderAbilityValue
            : 0;

    public static bool CanUseActiveAbility(Player player) =>
        GetKind(player) == PartyLeaderAbilityKind.StealFromHandOncePerTurn
        && !player.PartyLeaderRuntime.ActiveAbilityUsedThisTurn;

    public static void MarkActiveAbilityUsed(Player player) =>
        player.PartyLeaderRuntime.ActiveAbilityUsedThisTurn = true;

    public static void ResetTurnState(Player player) =>
        player.PartyLeaderRuntime.ActiveAbilityUsedThisTurn = false;

    public static void OnMagicCardPlayed(Game game, Player player, Action<int> drawCards)
    {
        if (GetKind(player) != PartyLeaderAbilityKind.OnMagicDraw) return;
        drawCards(GetDefinition(player)!.PartyLeaderAbilityValue);
    }
}
