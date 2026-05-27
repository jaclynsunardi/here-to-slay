using HereToSlay.Models;

namespace HereToSlay.Services;

public class GameService
{
    private readonly Random _rng = new();
    private readonly CardEffectExecutor _effects;
    private readonly PendingChoiceService _choices;

    public GameService()
    {
        _effects = new CardEffectExecutor(_rng);
        _choices = new PendingChoiceService(_rng);
        _choices.BindEffects(_effects);
        _effects.BindPendingChoices(_choices);
    }

    public GameViewDto ToView(Game game, string viewingPlayerId)
    {
        var winnerName = game.WinnerId == null
            ? null
            : game.Players.FirstOrDefault(p => p.Id == game.WinnerId)?.Name;

        return new GameViewDto(
            game.RoomCode,
            game.State,
            game.GameType,
            game.MaxPlayers,
            game.CurrentPlayerIndex,
            game.ActionPointsRemaining,
            game.WinnerId,
            winnerName,
            game.LastMessage,
            game.LastRollDie1,
            game.LastRollDie2,
            game.LastRollModifier,
            game.LastRollTotal,
            game.Deck.Count,
            game.MonsterDeck.Count,
            game.DiscardPile.Count,
            game.MonsterRow.Select(c => ToCardView(c)).ToList(),
            game.Players.Select(p => ToPlayerView(game, p, p.Id == viewingPlayerId, viewingPlayerId)).ToList(),
            viewingPlayerId,
            BuildPendingChoiceView(game, viewingPlayerId));
    }

    public void ResolvePendingChoice(
        Game game,
        string playerId,
        IReadOnlyList<string> selectedCardInstanceIds,
        string? selectedOption)
    {
        _choices.Resolve(game, playerId, selectedCardInstanceIds, selectedOption);
    }

    private static CardViewDto ToCardView(CardInstance card, bool isPartyLeader = false)
    {
        var def = CardCatalog.Get(card.DefinitionId);
        var leaderAbility = def.Type == CardType.PartyLeader || isPartyLeader
            ? def.PartyLeaderAbility.ToString()
            : null;
        var leaderValue = def.Type == CardType.PartyLeader || isPartyLeader
            ? def.PartyLeaderAbilityValue
            : (int?)null;
        var leaderAlt = def.Type == CardType.PartyLeader || isPartyLeader
            ? def.PartyLeaderAbilityAltValue
            : null;

        return new CardViewDto(
            card.InstanceId,
            card.DefinitionId,
            def.Name,
            def.Type.ToString(),
            def.HeroClass?.ToString(),
            def.RollThreshold,
            def.FailIfRollAtOrBelow,
            def.FailPenalty == AttackFailPenalty.None ? null : def.FailPenalty.ToString(),
            def.ImagePath,
            def.EffectText,
            def.PartyRequirements.Select(r =>
                new PartyRequirementDto(r.HeroClass?.ToString(), r.Count, r.GenericHero)).ToList(),
            card.AttachedItems.Select(i => ToCardView(i)).ToList(),
            isPartyLeader,
            leaderAbility,
            leaderValue,
            leaderAlt);
    }

    private static PlayerViewDto ToPlayerView(
        Game game,
        Player player,
        bool revealOwnHand,
        string viewingPlayerId)
    {
        var revealHand = revealOwnHand
            || ShouldRevealHandForPendingChoice(game, viewingPlayerId, player);

        var hand = revealHand
            ? player.Hand.Select(c => ToCardView(c)).ToList()
            : new List<CardViewDto>();

        return new PlayerViewDto(
            player.Id,
            player.Name,
            player.Type.ToString(),
            player.PartyLeader == null ? null : ToCardView(player.PartyLeader, true),
            hand,
            player.Party.Select(c => ToCardView(c)).ToList(),
            player.SlainMonsters.Select(c => ToCardView(c)).ToList(),
            player.Hand.Count,
            player.HeroesRolledThisTurn.ToList());
    }

    private static bool ShouldRevealHandForPendingChoice(Game game, string viewerId, Player handOwner)
    {
        var pending = game.PendingChoice;
        return pending != null
            && pending.PlayerId == viewerId
            && pending.Continuation.Type == PendingContinuationType.PickFromOpponentHand
            && pending.Continuation.TargetPlayerId == handOwner.Id;
    }

    private static PendingChoiceViewDto? BuildPendingChoiceView(Game game, string viewingPlayerId)
    {
        var pending = game.PendingChoice;
        if (pending == null) return null;

        var isYours = pending.PlayerId == viewingPlayerId;
        if (!isYours)
        {
            return new PendingChoiceViewDto(
                pending.Kind.ToString(),
                "Waiting for another player to choose…",
                0,
                0,
                [],
                [],
                false);
        }

        var cards = pending.Kind == PendingChoiceKind.SelectCards
            ? ResolveSelectableCards(game, pending).Select(c => ToCardView(c)).ToList()
            : [];

        return new PendingChoiceViewDto(
            pending.Kind.ToString(),
            pending.Prompt,
            pending.MinSelections,
            pending.MaxSelections,
            cards,
            pending.Options,
            true);
    }

    private static IEnumerable<CardInstance> ResolveSelectableCards(Game game, PendingChoiceState pending)
    {
        foreach (var id in pending.SelectableCardInstanceIds)
        {
            var card = FindCardByInstanceId(game, id);
            if (card != null) yield return card;
        }
    }

    private static CardInstance? FindCardByInstanceId(Game game, string instanceId)
    {
        foreach (var p in game.Players)
        {
            var inHand = p.Hand.FirstOrDefault(c => c.InstanceId == instanceId);
            if (inHand != null) return inHand;
            foreach (var hero in p.Party)
            {
                var onHero = hero.AttachedItems.FirstOrDefault(c => c.InstanceId == instanceId);
                if (onHero != null) return onHero;
            }
        }

        return game.DiscardPile.FirstOrDefault(c => c.InstanceId == instanceId)
            ?? game.ChoiceStaging.FirstOrDefault(c => c.InstanceId == instanceId)
            ?? game.Deck.FirstOrDefault(c => c.InstanceId == instanceId);
    }


    public void StartGame(Game game)
    {
        if (game.State != GameState.Waiting)
            throw new InvalidOperationException("Game already started.");

        if (game.Players.Count < 2)
            throw new InvalidOperationException("Need at least 2 players to start.");

        game.Deck = CardCatalog.BuildMainDeck(_rng);
        game.MonsterDeck = CardCatalog.BuildMonsterDeck(_rng);
        game.DiscardPile.Clear();
        game.MonsterRow.Clear();

        var shuffledLeaders = CardCatalog.PartyLeaderIds.OrderBy(_ => _rng.Next()).ToList();

        for (var i = 0; i < game.Players.Count; i++)
        {
            var player = game.Players[i];
            player.Hand.Clear();
            player.Party.Clear();
            player.SlainMonsters.Clear();
            player.HeroesRolledThisTurn.Clear();
            player.ChallengedByPlayerId = null;
            player.ActiveChallengeCardInstanceId = null;
            PartyLeaderAbilities.ResetTurnState(player);
            player.HeroTurnBuffs = new HeroTurnBuffs();

            var leaderId = shuffledLeaders[i % shuffledLeaders.Count];
            player.PartyLeader = new CardInstance { DefinitionId = leaderId };

            DrawCards(game, player, GameRules.StartingHandSize);
        }

        RefillMonsterRow(game);
        game.CurrentPlayerIndex = 0;
        game.State = GameState.InProgress;
        BeginTurn(game);
        game.LastMessage = $"{CurrentPlayer(game).Name} begins with {GameRules.ActionPointsPerTurn} action points.";
    }

    public void DrawCard(Game game, string playerId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);
        SpendActionPoints(game, GameRules.DrawCost);

        DrawCards(game, player, 1);
        game.LastMessage = $"{player.Name} draws a card. ({game.ActionPointsRemaining} AP left)";
    }

    public void PlayCard(
        Game game,
        string playerId,
        string cardInstanceId,
        string? targetHeroInstanceId,
        string? targetPlayerId,
        string? modifierCardInstanceId,
        bool _)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);

        var card = player.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId)
            ?? throw new InvalidOperationException("Card not in hand.");

        var def = CardCatalog.Get(card.DefinitionId);

        if (player.ChallengedByPlayerId != null && def.Type is CardType.Hero or CardType.Item)
            ResolveChallengeBeforePlay(game, player, () =>
                ExecutePlayCard(game, player, card, def, targetHeroInstanceId, targetPlayerId, modifierCardInstanceId));
        else
            ExecutePlayCard(game, player, card, def, targetHeroInstanceId, targetPlayerId, modifierCardInstanceId);
    }

    public void PlayChallenge(Game game, string playerId, string challengeCardInstanceId, string targetPlayerId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);
        SpendActionPoints(game, GameRules.PlayCardCost);

        var target = RequirePlayer(game, targetPlayerId);
        if (target.Id == player.Id)
            throw new InvalidOperationException("Cannot challenge yourself.");

        var card = player.Hand.FirstOrDefault(c => c.InstanceId == challengeCardInstanceId)
            ?? throw new InvalidOperationException("Card not in hand.");

        if (CardCatalog.Get(card.DefinitionId).Type != CardType.Challenge)
            throw new InvalidOperationException("Not a challenge card.");

        player.Hand.Remove(card);
        game.DiscardPile.Add(card);
        target.ChallengedByPlayerId = player.Id;
        target.ActiveChallengeCardInstanceId = card.InstanceId;
        game.LastMessage = $"{player.Name} challenges {target.Name}'s next Hero or Item play!";
    }

    public void RollHeroAbility(Game game, string playerId, string heroInstanceId, string? modifierCardInstanceId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);
        SpendActionPoints(game, GameRules.HeroRollCost);

        var hero = FindHeroInParty(player, heroInstanceId)
            ?? throw new InvalidOperationException("Hero not in your party.");

        if (player.HeroesRolledThisTurn.Contains(hero.InstanceId))
            throw new InvalidOperationException("You already used that hero's roll this turn.");

        var def = CardCatalog.Get(hero.DefinitionId);
        ResolveHeroAbilityRoll(
            game,
            player,
            hero,
            def,
            targetPlayerId: null,
            modifierCardInstanceId,
            markHeroRolledThisTurn: true);
    }

    public void AttackMonster(Game game, string playerId, string monsterInstanceId, string? modifierCardInstanceId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);
        SpendActionPoints(game, GameRules.AttackCost);

        var monster = game.MonsterRow.FirstOrDefault(c => c.InstanceId == monsterInstanceId)
            ?? throw new InvalidOperationException("Monster not in the row.");

        var monsterDef = CardCatalog.Get(monster.DefinitionId);
        if (!MeetsPartyRequirements(player, monsterDef))
            throw new InvalidOperationException("Party requirements not met for this monster.");

        var modifier = ConsumeModifier(game, player, modifierCardInstanceId);
        var (die1, die2, natural) = RollDiceNatural();
        var leaderBonus = PartyLeaderAbilities.AttackRollBonus(player);
        var total = natural + modifier + leaderBonus;
        SetRollDisplay(game, die1, die2, modifier + leaderBonus, total);

        if (monsterDef.FailIfRollAtOrBelow != null && total <= monsterDef.FailIfRollAtOrBelow)
        {
            _effects.ApplyAttackFailPenalty(game, player, monsterDef);
            game.LastMessage =
                $"Attack fails ({total} ≤ {monsterDef.FailIfRollAtOrBelow}) — {PenaltyText(monsterDef)}.";
            return;
        }

        if (total < monsterDef.RollThreshold)
        {
            game.LastMessage =
                $"No effect ({die1}+{die2}{(modifier > 0 ? $"+{modifier}" : "")}={total}, need {monsterDef.RollThreshold}+ to slay).";
            return;
        }

        game.MonsterRow.Remove(monster);
        player.SlainMonsters.Add(monster);
        RefillMonsterRow(game);
        game.LastMessage = $"Slayed {monsterDef.Name}! ({die1}+{die2}{(modifier > 0 ? $"+{modifier}" : "")}={total})";

        if (player.SlainMonsters.Count >= GameRules.MonstersToWin)
            DeclareWinner(game, player, "slaying 3 monsters");
    }

    public void DiscardHandAndRedraw(Game game, string playerId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);
        SpendActionPoints(game, GameRules.DiscardHandRedrawCost);

        while (player.Hand.Count > 0)
        {
            var c = player.Hand[0];
            player.Hand.RemoveAt(0);
            game.DiscardPile.Add(c);
        }

        DrawCards(game, player, GameRules.DiscardHandRedrawAmount);
        game.LastMessage = $"{player.Name} discards their hand and draws {GameRules.DiscardHandRedrawAmount} cards.";
    }

    public void EndTurn(Game game, string playerId)
    {
        var player = RequirePlayer(game, playerId);
        EnsureCurrentPlayer(game, player);

        if (HasFullParty(player))
            DeclareWinner(game, player, "a full party of six classes");

        if (game.State == GameState.Finished)
            return;

        game.CurrentPlayerIndex = (game.CurrentPlayerIndex + 1) % game.Players.Count;
        BeginTurn(game);
    }

    private void ExecutePlayCard(
        Game game,
        Player player,
        CardInstance card,
        CardDefinition def,
        string? targetHeroInstanceId,
        string? targetPlayerId,
        string? modifierCardInstanceId)
    {
        SpendActionPoints(game, GameRules.PlayCardCost);

        switch (def.Type)
        {
            case CardType.Hero:
                PlayHero(game, player, card, def);
                ResolveHeroAbilityRoll(
                    game,
                    player,
                    card,
                    def,
                    targetPlayerId,
                    modifierCardInstanceId,
                    markHeroRolledThisTurn: false);
                break;
            case CardType.Item:
                PlayItem(game, player, card, targetHeroInstanceId);
                break;
            case CardType.Magic:
                PlayMagic(game, player, card, def, targetPlayerId);
                break;
            case CardType.Modifier:
                throw new InvalidOperationException("Play modifiers during a roll, not as a main action.");
            case CardType.Challenge:
                throw new InvalidOperationException("Use the challenge action and select a target player.");
            default:
                throw new InvalidOperationException("Cannot play this card from hand.");
        }

        CheckWinAtEndOfAction(game, player);
    }

    private void PlayHero(Game game, Player player, CardInstance card, CardDefinition def)
    {
        if (def.HeroClass == null)
            throw new InvalidOperationException("Invalid hero.");

        if (GetPartyClasses(player).Contains(def.HeroClass.Value))
            throw new InvalidOperationException("You already have that class in your party (including masks).");

        player.Hand.Remove(card);
        player.Party.Add(card);
        game.LastMessage = $"{def.Name} joins the party!";
    }

    private void ResolveHeroAbilityRoll(
        Game game,
        Player player,
        CardInstance hero,
        CardDefinition def,
        string? targetPlayerId,
        string? modifierCardInstanceId,
        bool markHeroRolledThisTurn)
    {
        if (markHeroRolledThisTurn)
            player.HeroesRolledThisTurn.Add(hero.InstanceId);

        if (hero.AttachedItems.Any(i => CardCatalog.Get(i.DefinitionId).Id == "CursedItem-SealingKey"))
        {
            game.LastMessage = $"{def.Name} cannot use its effect (Sealing Key).";
            return;
        }

        var modifier = ConsumeModifier(game, player, modifierCardInstanceId);
        var (die1, die2, natural) = RollDiceNatural();
        var itemBonus = HeroRollItemBonus(hero);
        var leaderBonus = PartyLeaderAbilities.HeroRollBonus(player);
        var turnBonus = player.HeroTurnBuffs.RollBonusUntilEndOfTurn;
        var total = natural + modifier + itemBonus + leaderBonus + turnBonus;
        SetRollDisplay(game, die1, die2, modifier + itemBonus + leaderBonus + turnBonus, total);

        var minRoll = def.HeroEffectMinRoll;
        var bonusLabel = FormatRollBonuses(modifier, itemBonus, leaderBonus + turnBonus);

        if (string.IsNullOrWhiteSpace(def.EffectScript) || total < minRoll)
        {
            game.LastMessage =
                $"{game.LastMessage} Rolled {die1}+{die2}{bonusLabel}={total} — need {minRoll}+.";
            return;
        }

        _effects.Apply(game, player, def, targetPlayerId, hero);
        if (game.PendingChoice != null)
        {
            game.LastMessage += " Make your choice to continue.";
            return;
        }

        game.LastMessage =
            $"{game.LastMessage} Rolled {die1}+{die2}{bonusLabel}={total} — ability triggers!";
    }

    private void PlayItem(Game game, Player player, CardInstance card, string? targetHeroInstanceId)
    {
        if (string.IsNullOrEmpty(targetHeroInstanceId))
            throw new InvalidOperationException("Select a hero in your party to equip this item.");

        var hero = FindHeroInParty(player, targetHeroInstanceId)
            ?? throw new InvalidOperationException("Hero not in your party.");

        player.Hand.Remove(card);
        hero.AttachedItems.Add(card);
        game.LastMessage = $"Equipped {CardCatalog.Get(card.DefinitionId).Name} on {CardCatalog.Get(hero.DefinitionId).Name}.";
    }

    private void PlayMagic(Game game, Player player, CardInstance card, CardDefinition def, string? targetPlayerId)
    {
        player.Hand.Remove(card);
        game.DiscardPile.Add(card);
        _effects.Apply(game, player, def, targetPlayerId);
        if (game.PendingChoice != null)
            game.LastMessage += " Make your choice to continue.";
        PartyLeaderAbilities.OnMagicCardPlayed(game, player, n => DrawCards(game, player, n));
        game.LastMessage = $"Cast {def.Name}.";
    }

    private void ResolveChallengeBeforePlay(Game game, Player target, Action continuePlay)
    {
        if (target.HeroTurnBuffs.CardsCannotBeChallenged)
        {
            target.ChallengedByPlayerId = null;
            target.ActiveChallengeCardInstanceId = null;
            continuePlay();
            return;
        }

        var challengerId = target.ChallengedByPlayerId!;
        var challenger = RequirePlayer(game, challengerId);

        var challengeBonus = PartyLeaderAbilities.ChallengeRollBonus(challenger);
        var (c1, c2, cNatural) = RollDiceNatural();
        var cTotal = cNatural + challengeBonus;
        SetRollDisplay(game, c1, c2, challengeBonus, cTotal);
        var (t1, t2, tTotal) = RollDice(game, 0);

        target.ChallengedByPlayerId = null;
        target.ActiveChallengeCardInstanceId = null;

        if (tTotal < cTotal)
        {
            game.LastMessage =
                $"Challenge wins ({challenger.Name} {c1}+{c2}={cTotal} vs {target.Name} {t1}+{t2}={tTotal}) — play blocked!";
            throw new InvalidOperationException("Challenge succeeded — play cancelled.");
        }

        game.LastMessage =
            $"Challenge fails ({target.Name} {t1}+{t2}={tTotal} vs {challenger.Name} {c1}+{c2}={cTotal}) — play continues.";
        continuePlay();
    }

    private static string PenaltyText(CardDefinition monsterDef) => monsterDef.FailPenalty switch
    {
        AttackFailPenalty.SacrificeHero => "sacrifice a hero",
        AttackFailPenalty.Discard2 => "discard 2 cards",
        _ => "pay the penalty"
    };

    private static int HeroRollItemBonus(CardInstance hero) =>
        hero.AttachedItems.Sum(i =>
        {
            var id = CardCatalog.Get(i.DefinitionId).Id;
            return id switch
            {
                "Item-ReallyBigRing" => 2,
                "CursedItem-Snake'sEyes" => -2,
                _ => 0
            };
        });

    private void DeclareWinner(Game game, Player player, string reason)
    {
        game.State = GameState.Finished;
        game.WinnerId = player.Id;
        game.LastMessage = $"{player.Name} wins by {reason}!";
    }

    private void CheckWinAtEndOfAction(Game game, Player player)
    {
        if (player.SlainMonsters.Count >= GameRules.MonstersToWin)
            DeclareWinner(game, player, "slaying 3 monsters");
    }

    private static bool HasFullParty(Player player)
    {
        return GetPartyClasses(player).Count >= GameRules.ClassesToWin;
    }

    private static HashSet<HeroClass> GetPartyClasses(Player player)
    {
        var classes = new HashSet<HeroClass>();
        if (player.PartyLeader != null)
        {
            var leaderClass = CardCatalog.Get(player.PartyLeader.DefinitionId).HeroClass;
            if (leaderClass != null) classes.Add(leaderClass.Value);
        }

        foreach (var hero in player.Party)
        {
            var hc = GetEffectiveHeroClass(hero);
            if (hc != null) classes.Add(hc.Value);
        }

        return classes;
    }

    private static HeroClass? GetEffectiveHeroClass(CardInstance hero)
    {
        var def = CardCatalog.Get(hero.DefinitionId);
        var mask = hero.AttachedItems.Select(i => CardCatalog.Get(i.DefinitionId).Id).FirstOrDefault(id =>
            id is "Item-BardMask" or "Item-RangerMask" or "Item-ThiefMask" or "Item-WizardMask"
                or "Item-GuardianMask" or "Item-FighterMask");
        return mask switch
        {
            "Item-BardMask" => HeroClass.Bard,
            "Item-RangerMask" => HeroClass.Ranger,
            "Item-ThiefMask" => HeroClass.Thief,
            "Item-WizardMask" => HeroClass.Wizard,
            "Item-GuardianMask" => HeroClass.Guardian,
            "Item-FighterMask" => HeroClass.Fighter,
            _ => def.HeroClass
        };
    }

    private static bool MeetsPartyRequirements(Player player, CardDefinition monsterDef)
    {
        foreach (var req in monsterDef.PartyRequirements)
        {
            var count = req.GenericHero
                ? player.Party.Count
                : CountClassForMonsterAttack(player, req.HeroClass);
            if (count < req.Count)
                return false;
        }

        return true;
    }

    /// <summary>Class icons: party leader counts; generic Hero icons do not.</summary>
    private static int CountClassForMonsterAttack(Player player, HeroClass? heroClass)
    {
        var count = 0;
        if (player.PartyLeader != null && heroClass != null)
        {
            var lc = CardCatalog.Get(player.PartyLeader.DefinitionId).HeroClass;
            if (lc == heroClass) count++;
        }

        foreach (var hero in player.Party)
        {
            if (GetEffectiveHeroClass(hero) == heroClass) count++;
        }

        return count;
    }

    private static int ConsumeModifier(Game game, Player player, string? modifierCardInstanceId)
    {
        if (string.IsNullOrEmpty(modifierCardInstanceId))
            return 0;

        var mod = player.Hand.FirstOrDefault(c => c.InstanceId == modifierCardInstanceId)
            ?? throw new InvalidOperationException("Modifier not in hand.");

        var def = CardCatalog.Get(mod.DefinitionId);
        if (def.Type != CardType.Modifier)
            throw new InvalidOperationException("Not a modifier card.");

        player.Hand.Remove(mod);
        game.DiscardPile.Add(mod);
        return def.ModifierBonus;
    }

    private (int die1, int die2, int total) RollDice(Game game, int modifier)
    {
        var die1 = _rng.Next(1, 7);
        var die2 = _rng.Next(1, 7);
        var total = die1 + die2 + modifier;
        SetRollDisplay(game, die1, die2, modifier, total);
        return (die1, die2, total);
    }

    private (int die1, int die2, int natural) RollDiceNatural() =>
        RollDiceNatural(_rng);

    private static (int die1, int die2, int natural) RollDiceNatural(Random rng)
    {
        var die1 = rng.Next(1, 7);
        var die2 = rng.Next(1, 7);
        return (die1, die2, die1 + die2);
    }

    private void SetRollDisplay(Game game, int die1, int die2, int modifier, int total)
    {
        game.LastRollDie1 = die1;
        game.LastRollDie2 = die2;
        game.LastRollModifier = modifier;
        game.LastRollTotal = total;
    }

    private static string FormatRollBonuses(int modifier, int itemBonus, int leaderBonus)
    {
        var extra = modifier + itemBonus + leaderBonus;
        return extra > 0 ? $"+{extra}" : extra < 0 ? extra.ToString() : "";
    }

    private void BeginTurn(Game game)
    {
        var player = CurrentPlayer(game);
        player.HeroesRolledThisTurn.Clear();
        PartyLeaderAbilities.ResetTurnState(player);
        player.HeroTurnBuffs = new HeroTurnBuffs();
        game.ActionPointsRemaining = GameRules.ActionPointsPerTurn;
        game.LastRollDie1 = null;
        game.LastRollDie2 = null;
        game.LastRollModifier = null;
        game.LastRollTotal = null;
        game.LastMessage = $"{player.Name}'s turn — {game.ActionPointsRemaining} action points.";
    }

    private static void SpendActionPoints(Game game, int cost)
    {
        if (game.ActionPointsRemaining < cost)
            throw new InvalidOperationException($"Not enough action points (need {cost}, have {game.ActionPointsRemaining}).");

        game.ActionPointsRemaining -= cost;
    }

    private void DrawCards(Game game, Player player, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (game.Deck.Count == 0)
                ReshuffleDiscard(game);

            if (game.Deck.Count == 0)
                return;

            var drawn = game.Deck[^1];
            game.Deck.RemoveAt(game.Deck.Count - 1);
            player.Hand.Add(drawn);
        }
    }

    private void DiscardRandomFromHand(Game game, Player player, int count)
    {
        for (var i = 0; i < count && player.Hand.Count > 0; i++)
        {
            var card = player.Hand[_rng.Next(player.Hand.Count)];
            player.Hand.Remove(card);
            game.DiscardPile.Add(card);
        }
    }

    private void ReshuffleDiscard(Game game)
    {
        if (game.DiscardPile.Count == 0)
            return;

        game.Deck.AddRange(game.DiscardPile);
        game.DiscardPile.Clear();

        for (var i = game.Deck.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (game.Deck[i], game.Deck[j]) = (game.Deck[j], game.Deck[i]);
        }
    }

    private void RefillMonsterRow(Game game)
    {
        while (game.MonsterRow.Count < GameRules.MonsterRowSize && game.MonsterDeck.Count > 0)
        {
            var monster = game.MonsterDeck[^1];
            game.MonsterDeck.RemoveAt(game.MonsterDeck.Count - 1);
            game.MonsterRow.Add(monster);
        }
    }

    private static CardInstance? FindHeroInParty(Player player, string heroInstanceId) =>
        player.Party.FirstOrDefault(h => h.InstanceId == heroInstanceId);

    private static Player CurrentPlayer(Game game) => game.Players[game.CurrentPlayerIndex];

    private static Player RequirePlayer(Game game, string playerId) =>
        game.Players.FirstOrDefault(p => p.Id == playerId)
        ?? throw new InvalidOperationException("Player not in game.");

    private static void EnsureCurrentPlayer(Game game, Player player)
    {
        if (game.State != GameState.InProgress)
            throw new InvalidOperationException("Game is not in progress.");

        PendingChoiceService.EnsureNoPendingChoice(game, player.Id);

        if (CurrentPlayer(game).Id != player.Id)
            throw new InvalidOperationException("Not your turn.");
    }
}
