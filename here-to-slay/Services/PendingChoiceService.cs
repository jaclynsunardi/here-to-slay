using HereToSlay.Models;

namespace HereToSlay.Services;

public class PendingChoiceService
{
    private readonly Random _rng;
    private CardEffectExecutor? _effects;

    public PendingChoiceService(Random rng) => _rng = rng;

    public void BindEffects(CardEffectExecutor effects) => _effects = effects;

    public static bool HasPendingFor(Game game, string playerId) =>
        game.PendingChoice?.PlayerId == playerId;

    public static void EnsureNoPendingChoice(Game game, string playerId)
    {
        if (game.PendingChoice == null) return;
        if (game.PendingChoice.PlayerId == playerId)
            throw new InvalidOperationException("Finish your pending choice before taking another action.");
        throw new InvalidOperationException("Waiting for another player to make a choice.");
    }

    public void RequestSelectCards(
        Game game,
        Player player,
        string prompt,
        IEnumerable<string> selectableInstanceIds,
        int min,
        int max,
        PendingContinuation continuation)
    {
        var ids = selectableInstanceIds.Distinct().ToList();
        if (ids.Count == 0 && min > 0)
            throw new InvalidOperationException("No valid cards to choose from.");

        game.PendingChoice = new PendingChoiceState
        {
            PlayerId = player.Id,
            Kind = PendingChoiceKind.SelectCards,
            Prompt = prompt,
            MinSelections = min,
            MaxSelections = max,
            SelectableCardInstanceIds = ids,
            Continuation = continuation
        };
    }

    public void RequestSelectOption(
        Game game,
        Player player,
        string prompt,
        IReadOnlyList<string> options,
        PendingContinuation continuation)
    {
        game.PendingChoice = new PendingChoiceState
        {
            PlayerId = player.Id,
            Kind = PendingChoiceKind.SelectOption,
            Prompt = prompt,
            MinSelections = 1,
            MaxSelections = 1,
            Options = options.ToList(),
            Continuation = continuation
        };
    }

    public void Resolve(
        Game game,
        string playerId,
        IReadOnlyList<string> selectedCardInstanceIds,
        string? selectedOption)
    {
        var pending = game.PendingChoice
            ?? throw new InvalidOperationException("No choice is waiting.");

        if (pending.PlayerId != playerId)
            throw new InvalidOperationException("It is not your choice to make.");

        game.PendingChoice = null;

        switch (pending.Kind)
        {
            case PendingChoiceKind.SelectCards:
                ResolveCardSelection(game, playerId, pending, selectedCardInstanceIds);
                break;
            case PendingChoiceKind.SelectOption:
                if (string.IsNullOrWhiteSpace(selectedOption) || !pending.Options.Contains(selectedOption))
                    throw new InvalidOperationException("Invalid option selected.");
                ResolveOptionSelection(game, playerId, pending, selectedOption);
                break;
            default:
                throw new InvalidOperationException("Unsupported pending choice type.");
        }
    }

    private void ResolveCardSelection(
        Game game,
        string playerId,
        PendingChoiceState pending,
        IReadOnlyList<string> selectedCardInstanceIds)
    {
        var player = game.Players.First(p => p.Id == playerId);
        var selected = selectedCardInstanceIds.Distinct().ToList();

        if (selected.Count < pending.MinSelections || selected.Count > pending.MaxSelections)
            throw new InvalidOperationException(
                $"Select between {pending.MinSelections} and {pending.MaxSelections} card(s).");

        foreach (var id in selected)
        {
            if (!pending.SelectableCardInstanceIds.Contains(id))
                throw new InvalidOperationException("Invalid card selection.");
        }

        switch (pending.Continuation.Type)
        {
            case PendingContinuationType.QiBearDestroy:
                DiscardSpecificFromHand(game, player, selected);
                for (var i = 0; i < selected.Count; i++)
                    Effects.DestroyHeroOnOpponentPublic(game, player, pending.Continuation.TargetPlayerId);
                game.LastMessage += $" Discarded {selected.Count} card(s) and destroyed {selected.Count} hero(es).";
                break;

            case PendingContinuationType.BearyWisePickFromPool:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose exactly one card.");
                TakeStagedCard(game, player, pending, selected[0]);
                game.LastMessage += " Added chosen card to your hand.";
                break;

            case PendingContinuationType.ScryPickOne:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose exactly one card.");
                CompleteScry(game, player, pending, selected[0]);
                game.LastMessage += " Added chosen card to your hand.";
                break;

            case PendingContinuationType.PickFromDiscard:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose exactly one card.");
                TakeFromDiscardPile(game, player, selected[0]);
                game.LastMessage += " Added card from discard to your hand.";
                break;

            case PendingContinuationType.DiscardExactCount:
                DiscardSpecificFromHand(game, player, selected);
                game.LastMessage += $" Discarded {selected.Count} card(s).";
                break;

            case PendingContinuationType.PickFromOpponentHand:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose exactly one card.");
                StealSpecificFromHand(game, player, pending.Continuation.TargetPlayerId!, selected[0]);
                game.LastMessage += " Took chosen card.";
                break;

            case PendingContinuationType.ReturnCursedItem:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose exactly one cursed item.");
                ReturnCursedItemToHand(game, player, selected[0]);
                game.LastMessage += " Returned cursed item to your hand.";
                break;

            case PendingContinuationType.SlipperyPawsDiscardOne:
                if (selected.Count != 1)
                    throw new InvalidOperationException("Choose one card to discard.");
                DiscardSpecificFromHand(game, player, selected);
                game.LastMessage += " Discarded chosen card.";
                break;

            default:
                throw new InvalidOperationException("Unknown pending continuation.");
        }
    }

    private void ResolveOptionSelection(
        Game game,
        string playerId,
        PendingChoiceState pending,
        string selectedOption)
    {
        var player = game.Players.First(p => p.Id == playerId);

        switch (pending.Continuation.Type)
        {
            case PendingContinuationType.OptionalPlayHeroFromHand:
                if (selectedOption == "yes")
                    Effects.TryPlayHeroFromHandPublic(game, player);
                break;

            case PendingContinuationType.OptionalPlayItemFromHand:
                if (selectedOption == "yes")
                    Effects.TryPlayItemFromHandPublic(game, player);
                break;

            case PendingContinuationType.OptionalDestroyHero:
                if (selectedOption == "yes")
                    Effects.DestroyHeroOnOpponentPublic(game, player, pending.Continuation.TargetPlayerId);
                break;

            case PendingContinuationType.ModifierSign:
                if (!int.TryParse(selectedOption, out var mod))
                    throw new InvalidOperationException("Invalid modifier value.");
                game.LastRollModifier = (game.LastRollModifier ?? 0) + mod;
                game.LastRollTotal = (game.LastRollTotal ?? 0) + mod;
                break;

            default:
                throw new InvalidOperationException("Unknown pending continuation.");
        }
    }

    private static void DiscardSpecificFromHand(Game game, Player player, List<string> instanceIds)
    {
        foreach (var id in instanceIds)
        {
            var card = player.Hand.FirstOrDefault(c => c.InstanceId == id)
                ?? throw new InvalidOperationException("Card not in hand.");
            player.Hand.Remove(card);
            game.DiscardPile.Add(card);
        }
    }

    private static void TakeFromDiscardPile(Game game, Player player, string instanceId)
    {
        var card = game.DiscardPile.FirstOrDefault(c => c.InstanceId == instanceId)
            ?? throw new InvalidOperationException("Card not in discard pile.");
        game.DiscardPile.Remove(card);
        player.Hand.Add(card);
    }

    private static void StealSpecificFromHand(Game game, Player chooser, string fromPlayerId, string instanceId)
    {
        var from = game.Players.First(p => p.Id == fromPlayerId);
        var card = from.Hand.FirstOrDefault(c => c.InstanceId == instanceId)
            ?? throw new InvalidOperationException("Card not in that player's hand.");
        from.Hand.Remove(card);
        chooser.Hand.Add(card);
    }

    private static void ReturnCursedItemToHand(Game game, Player player, string itemInstanceId)
    {
        foreach (var hero in player.Party)
        {
            var item = hero.AttachedItems.FirstOrDefault(i => i.InstanceId == itemInstanceId);
            if (item == null) continue;
            hero.AttachedItems.Remove(item);
            player.Hand.Add(item);
            return;
        }

        throw new InvalidOperationException("Cursed item not found on your party.");
    }

    private static void TakeStagedCard(Game game, Player player, PendingChoiceState pending, string instanceId)
    {
        var card = FindStagedCard(game, pending, instanceId);
        player.Hand.Add(card);
    }

    private void CompleteScry(Game game, Player player, PendingChoiceState pending, string chosenId)
    {
        var chosen = FindStagedCard(game, pending, chosenId);
        player.Hand.Add(chosen);

        foreach (var card in game.ChoiceStaging.ToList())
            game.Deck.Add(card);
        game.ChoiceStaging.Clear();
    }

    private static CardInstance FindStagedCard(Game game, PendingChoiceState pending, string instanceId)
    {
        var staged = game.ChoiceStaging.FirstOrDefault(c => c.InstanceId == instanceId);
        if (staged != null)
        {
            game.ChoiceStaging.Remove(staged);
            return staged;
        }

        var fromDiscard = game.DiscardPile.FirstOrDefault(c => c.InstanceId == instanceId);
        if (fromDiscard != null)
        {
            game.DiscardPile.Remove(fromDiscard);
            return fromDiscard;
        }

        throw new InvalidOperationException("Staged card not found.");
    }

    private CardEffectExecutor Effects =>
        _effects ?? throw new InvalidOperationException("Effect executor not initialized.");
}
