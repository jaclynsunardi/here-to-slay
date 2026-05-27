using HereToSlay.Models;

namespace HereToSlay.Services;

/// <summary>Resolves card effects from effectText / effectScript using keyword rules.</summary>
public class CardEffectExecutor
{
    private readonly Random _rng;
    private PendingChoiceService? _choices;

    public CardEffectExecutor(Random rng) => _rng = rng;

    public void BindPendingChoices(PendingChoiceService choices) => _choices = choices;

    public void Apply(Game game, Player player, CardDefinition def, string? targetPlayerId = null, CardInstance? sourceHero = null)
    {
        if (game.PendingChoice != null) return;

        var script = string.IsNullOrWhiteSpace(def.EffectScript) ? def.EffectText : def.EffectScript;
        if (string.IsNullOrWhiteSpace(script)) return;

        var t = script.ToUpperInvariant();
        var target = ResolveTarget(game, player, targetPlayerId);

        if (t.Contains("DO NOTHING"))
            return;

        if (t.Contains("+5 TO ALL OF YOUR ROLLS"))
        {
            player.HeroTurnBuffs.RollBonusUntilEndOfTurn = Math.Max(player.HeroTurnBuffs.RollBonusUntilEndOfTurn, 5);
            return;
        }

        if (t.Contains("+3 TO ALL OF YOUR ROLLS"))
        {
            player.HeroTurnBuffs.RollBonusUntilEndOfTurn = Math.Max(player.HeroTurnBuffs.RollBonusUntilEndOfTurn, 3);
            return;
        }

        if (t.Contains("CANNOT BE STOLEN"))
        {
            player.HeroTurnBuffs.HeroesCannotBeStolen = true;
            return;
        }

        if (t.Contains("CANNOT BE DESTROYED"))
        {
            player.HeroTurnBuffs.HeroesCannotBeDestroyed = true;
            return;
        }

        if (t.Contains("CANNOT BE CHALLENGED"))
        {
            player.HeroTurnBuffs.CardsCannotBeChallenged = true;
            return;
        }

        if (t.Contains("DISCARD UP TO 3") && t.Contains("DESTROY A HERO"))
        {
            RequestCardChoice(
                game,
                player,
                "Discard up to 3 cards from your hand (each one lets you destroy a hero).",
                player.Hand.Select(c => c.InstanceId),
                0,
                Math.Min(3, player.Hand.Count),
                new PendingContinuation
                {
                    Type = PendingContinuationType.QiBearDestroy,
                    TargetPlayerId = targetPlayerId
                });
            return;
        }

        if (t.Contains("DRAW 3") && t.Contains("DISCARD"))
        {
            DrawCards(game, player, 3);
            if (player.Hand.Count == 0) return;
            RequestCardChoice(
                game,
                player,
                "Choose 1 card to discard.",
                player.Hand.Select(c => c.InstanceId),
                1,
                1,
                new PendingContinuation { Type = PendingContinuationType.DiscardExactCount });
            return;
        }

        if (t.Contains("DRAW") && t.Contains("UNTIL") && t.Contains("7"))
        {
            DrawUntilHandSize(game, player, 7);
            return;
        }

        if (t.Contains("DRAW A CARD") && t.Contains("MAGIC") && t.Contains("PLAY"))
        {
            DrawCards(game, player, 1);
            TryPlayMagicFromHand(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("DRAW A CARD") && t.Contains("HERO") && t.Contains("PLAY"))
        {
            DrawCards(game, player, 1);
            if (player.Hand.Any(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Hero))
            {
                RequestOptionChoice(
                    game,
                    player,
                    "You drew a Hero card. Play a hero from your hand now?",
                    ["yes", "no"],
                    new PendingContinuation { Type = PendingContinuationType.OptionalPlayHeroFromHand });
            }
            return;
        }

        if (t.Contains("DRAW 2") && t.Contains("CHALLENGE") && t.Contains("DESTROY"))
        {
            DrawCards(game, player, 2);
            if (player.Hand.Any(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Challenge))
            {
                RequestOptionChoice(
                    game,
                    player,
                    "You drew a Challenge card. Destroy a hero?",
                    ["yes", "no"],
                    new PendingContinuation
                    {
                        Type = PendingContinuationType.OptionalDestroyHero,
                        TargetPlayerId = targetPlayerId
                    });
            }
            return;
        }

        if (t.Contains("DRAW 2") && t.Contains("ITEM") && t.Contains("PLAY"))
        {
            DrawCards(game, player, 2);
            if (player.Hand.Any(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Item))
            {
                RequestOptionChoice(
                    game,
                    player,
                    "You drew an Item card. Play an item from your hand now?",
                    ["yes", "no"],
                    new PendingContinuation { Type = PendingContinuationType.OptionalPlayItemFromHand });
            }
            return;
        }

        if (t.Contains("PLAY AN ITEM") && t.Contains("DRAW"))
        {
            TryPlayItemFromHand(game, player);
            DrawCards(game, player, 1);
            return;
        }

        if (t.Contains("DRAW 2") && t.Contains("DISCARD 2"))
        {
            DrawCards(game, player, 2);
            DiscardFromHand(game, player, 2, playerChooses: false);
            return;
        }

        if (t.Contains("DRAW 2") && !t.Contains("IF"))
        {
            DrawCards(game, player, 2);
            return;
        }

        if (t.Contains("DESTROY A HERO") && t.Contains("DRAW A CARD"))
        {
            DestroyHeroOnOpponent(game, player, targetPlayerId);
            DrawCards(game, player, 1);
            return;
        }

        if (t.Contains("DESTROY A HERO") && t.Contains("ITEM CARD") && t.Contains("HAND"))
        {
            DestroyHeroOnOpponentWithItemReward(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("STEAL A HERO") && t.Contains("ROLL TO USE"))
        {
            if (StealHeroFromOpponent(game, player, targetPlayerId, out var stolen) && stolen != null)
            {
                var stolenDef = CardCatalog.Get(stolen.DefinitionId);
                if (stolenDef.Type == CardType.Hero)
                    Apply(game, player, stolenDef, targetPlayerId, stolen);
            }
            return;
        }

        if (t.Contains("STEAL A HERO") && t.Contains("DESTROY A HERO"))
        {
            StealHeroFromOpponent(game, player, targetPlayerId);
            DestroyHeroOnOpponent(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("STEAL A HERO") && (t.Contains("PULL A CARD") || t.Contains("PULL A CARD FROM")))
        {
            if (StealHeroFromOpponent(game, player, targetPlayerId) && target != null)
                StealRandomFromHand(game, target, player, 1);
            return;
        }

        if (t.Contains("MOVE") && t.Contains("TO THAT PLAYER'S PARTY"))
        {
            StealHeroSwapWithSource(game, player, targetPlayerId, sourceHero);
            return;
        }

        if (t.Contains("PULL 2") && t.Contains("DISCARD ONE"))
        {
            if (target != null)
            {
                var pulled = PullSpecificFromHand(game, target, player, 2);
                if (pulled.Count > 0)
                {
                    RequestCardChoice(
                        game,
                        player,
                        "Choose 1 of the pulled cards to discard.",
                        pulled,
                        1,
                        1,
                        new PendingContinuation { Type = PendingContinuationType.SlipperyPawsDiscardOne });
                }
            }
            return;
        }

        if (t.Contains("PULL 2") && t.Contains("MAY DRAW"))
        {
            if (target != null)
            {
                StealRandomFromHand(game, target, player, 2);
                DrawCards(game, target, 1);
            }
            return;
        }

        if (t.Contains("THIEF IN THEIR PARTY"))
        {
            foreach (var p in game.Players.Where(p => p.Id != player.Id && PartyHasClass(p, HeroClass.Thief)))
                StealRandomFromHand(game, p, player, 1);
            return;
        }

        if (t.Contains("LOOK AT ANOTHER PLAYER'S HAND") && t.Contains("CHOOSE A CARD"))
        {
            if (target != null && target.Hand.Count > 0)
            {
                RequestCardChoice(
                    game,
                    player,
                    $"Choose a card to take from {target.Name}'s hand.",
                    target.Hand.Select(c => c.InstanceId),
                    1,
                    1,
                    new PendingContinuation
                    {
                        Type = PendingContinuationType.PickFromOpponentHand,
                        TargetPlayerId = target.Id
                    });
            }
            return;
        }

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("ITEM"))
        {
            RequestPickFromDiscard(game, player, CardType.Item);
            return;
        }

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("MODIFIER"))
        {
            RequestPickFromDiscard(game, player, CardType.Modifier);
            return;
        }

        if (t.Contains("TRADE HANDS"))
        {
            if (target != null) TradeHands(player, target);
            return;
        }

        if (t.Contains("LOOK AT THE TOP 3"))
        {
            if (game.Deck.Count < 3) return;
            var top = game.Deck.TakeLast(3).ToList();
            foreach (var c in top)
                game.Deck.Remove(c);
            game.ChoiceStaging.AddRange(top);
            RequestCardChoice(
                game,
                player,
                "Choose 1 card to add to your hand (the rest go back on top of the deck).",
                top.Select(c => c.InstanceId),
                1,
                1,
                new PendingContinuation
                {
                    Type = PendingContinuationType.ScryPickOne,
                    StagedCardInstanceIds = top.Select(c => c.InstanceId).ToList()
                });
            return;
        }

        if (t.Contains("DRAW A CARD") || t.Contains("DRAW 1") || (t.StartsWith("DRAW") && !t.Contains("DISCARD") && !t.Contains("MAGIC") && !t.Contains("UNTIL")))
        {
            var n = ExtractCount(t, "DRAW", defaultCount: 1);
            DrawCards(game, player, n);
            return;
        }

        if (t.Contains("ALL PLAYERS") && t.Contains("DRAW"))
        {
            foreach (var p in game.Players)
                DrawCards(game, p, 1);
            return;
        }

        if (t.Contains("EACH OTHER PLAYER") && t.Contains("DISCARD") && t.Contains("CHOOSE"))
        {
            var pool = new List<CardInstance>();
            foreach (var p in game.Players.Where(p => p.Id != player.Id))
                pool.AddRange(DiscardFromHand(game, p, 1, playerChooses: false));
            if (pool.Count == 0) return;
            RequestCardChoice(
                game,
                player,
                "Choose one of the cards each opponent discarded.",
                pool.Select(c => c.InstanceId),
                1,
                1,
                new PendingContinuation
                {
                    Type = PendingContinuationType.BearyWisePickFromPool,
                    StagedCardInstanceIds = pool.Select(c => c.InstanceId).ToList()
                });
            return;
        }

        if (t.Contains("EACH OTHER PLAYER") && t.Contains("GIVE YOU"))
        {
            foreach (var p in game.Players.Where(p => p.Id != player.Id))
                StealRandomFromHand(game, p, player, 1);
            return;
        }

        if (t.Contains("EACH OTHER PLAYER") && t.Contains("SACRIFICE"))
        {
            foreach (var p in game.Players.Where(p => p.Id != player.Id))
                SacrificeRandomHero(game, p);
            return;
        }

        if (t.Contains("CHOOSE A PLAYER") && t.Contains("DISCARD 2"))
        {
            if (target != null && target.Hand.Count > 0)
            {
                var max = Math.Min(2, target.Hand.Count);
                RequestCardChoice(
                    game,
                    target,
                    "Choose 2 cards to discard.",
                    target.Hand.Select(c => c.InstanceId),
                    max,
                    max,
                    new PendingContinuation { Type = PendingContinuationType.DiscardExactCount });
            }
            return;
        }

        if (t.Contains("CHOOSE A PLAYER") && t.Contains("SACRIFICE"))
        {
            if (target != null) SacrificeRandomHero(game, target);
            return;
        }

        if (t.Contains("WITH A FIGHTER") && t.Contains("DISCARD"))
        {
            foreach (var p in game.Players.Where(p => p.Id != player.Id && PartyHasClass(p, HeroClass.Fighter)))
                DiscardFromHand(game, p, 1, playerChooses: false);
            return;
        }

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("HERO"))
        {
            RequestPickFromDiscard(game, player, CardType.Hero);
            return;
        }

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("MAGIC"))
        {
            RequestPickFromDiscard(game, player, CardType.Magic);
            return;
        }

        if (t.Contains("RETURN A CURSED ITEM"))
        {
            var cursed = ListCursedItemsOnParty(player);
            if (cursed.Count == 0) return;
            if (cursed.Count == 1)
            {
                ReturnCursedItemInstance(player, cursed[0]);
                return;
            }

            RequestCardChoice(
                game,
                player,
                "Choose a cursed item to return to your hand.",
                cursed,
                1,
                1,
                new PendingContinuation { Type = PendingContinuationType.ReturnCursedItem });
            return;
        }

        if (t.Contains("PULL A CARD") || (t.Contains("PULL") && t.Contains("HAND")) || (t.Contains("STEAL") && t.Contains("HAND")))
        {
            var pullHeroBonus = t.Contains("IF IT IS A HERO") && t.Contains("SECOND");
            var pullMagicBonus = t.Contains("IF IT IS A MAGIC");
            var pullChallengeBonus = t.Contains("IF IT IS A CHALLENGE");
            if (target != null)
                PullFromHandWithBonus(game, target, player, pullHeroBonus, pullMagicBonus, pullChallengeBonus);
            return;
        }

        if (t.Contains("DESTROY 2 HERO"))
        {
            DestroyHeroOnOpponent(game, player, targetPlayerId);
            DestroyHeroOnOpponent(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("DESTROY A HERO") || t.Contains("DESTROY A HERO CARD"))
        {
            DestroyHeroOnOpponent(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("STEAL A HERO") || (t.Contains("STEAL") && t.Contains("PARTY")))
        {
            StealHeroFromOpponent(game, player, targetPlayerId);
            return;
        }

        if (t.Contains("SACRIFICE") && t.Contains("DRAW 2"))
        {
            if (player.Party.Count > 0)
            {
                SacrificeRandomHero(game, player);
                DrawCards(game, player, 2);
            }
            return;
        }

        if (t.Contains("PLAY A HERO") && t.Contains("DRAW"))
        {
            DrawCards(game, player, 1);
            TryPlayHeroFromHand(game, player);
            return;
        }
    }

    public void ApplyAttackFailPenalty(Game game, Player player, CardDefinition monsterDef)
    {
        switch (monsterDef.FailPenalty)
        {
            case AttackFailPenalty.SacrificeHero:
                SacrificeRandomHero(game, player);
                break;
            case AttackFailPenalty.Discard2:
                DiscardFromHand(game, player, 2, playerChooses: false);
                break;
        }
    }

    private Player? ResolveTarget(Game game, Player player, string? targetPlayerId)
    {
        if (!string.IsNullOrEmpty(targetPlayerId))
            return game.Players.FirstOrDefault(p => p.Id == targetPlayerId);
        return game.Players.FirstOrDefault(p => p.Id != player.Id);
    }

    private static int ExtractCount(string t, string keyword, int defaultCount)
    {
        var idx = t.IndexOf(keyword, StringComparison.Ordinal);
        if (idx < 0) return defaultCount;
        var rest = t[(idx + keyword.Length)..].TrimStart();
        var digits = new string(rest.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : defaultCount;
    }

    private static bool PartyHasClass(Player player, HeroClass heroClass)
    {
        if (player.PartyLeader != null)
        {
            var lc = CardCatalog.Get(player.PartyLeader.DefinitionId).HeroClass;
            if (lc == heroClass) return true;
        }

        return player.Party.Any(h => CardCatalog.Get(h.DefinitionId).HeroClass == heroClass);
    }

    private void DrawCards(Game game, Player player, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (game.Deck.Count == 0) ReshuffleDiscard(game);
            if (game.Deck.Count == 0) return;
            var drawn = game.Deck[^1];
            game.Deck.RemoveAt(game.Deck.Count - 1);
            player.Hand.Add(drawn);
        }
    }

    private void ReshuffleDiscard(Game game)
    {
        if (game.DiscardPile.Count == 0) return;
        game.Deck.AddRange(game.DiscardPile);
        game.DiscardPile.Clear();
        for (var i = game.Deck.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (game.Deck[i], game.Deck[j]) = (game.Deck[j], game.Deck[i]);
        }
    }

    private List<CardInstance> DiscardFromHand(Game game, Player player, int count, bool playerChooses)
    {
        var discarded = new List<CardInstance>();
        for (var i = 0; i < count && player.Hand.Count > 0; i++)
        {
            var card = player.Hand[_rng.Next(player.Hand.Count)];
            player.Hand.Remove(card);
            game.DiscardPile.Add(card);
            discarded.Add(card);
        }
        return discarded;
    }

    private void DrawUntilHandSize(Game game, Player player, int size)
    {
        while (player.Hand.Count < size)
        {
            if (game.Deck.Count == 0) ReshuffleDiscard(game);
            if (game.Deck.Count == 0) break;
            DrawCards(game, player, 1);
        }
    }

    private void TradeHands(Player a, Player b)
    {
        (a.Hand, b.Hand) = (b.Hand, a.Hand);
    }

    private void ScryThree(Game game, Player player)
    {
        if (game.Deck.Count < 3)
            return;
        var top = game.Deck.TakeLast(3).ToList();
        foreach (var c in top)
            game.Deck.Remove(c);
        player.Hand.Add(top[0]);
        game.Deck.Add(top[1]);
        game.Deck.Add(top[2]);
    }

    private void TakeFromDiscard(Game game, Player player, CardType type)
    {
        var match = game.DiscardPile.LastOrDefault(c => CardCatalog.Get(c.DefinitionId).Type == type);
        if (match == null) return;
        game.DiscardPile.Remove(match);
        player.Hand.Add(match);
    }

    private static void ReturnCursedItemToHand(Player player)
    {
        foreach (var hero in player.Party)
        {
            var cursed = hero.AttachedItems.FirstOrDefault(i =>
                CardCatalog.Get(i.DefinitionId).Id.StartsWith("CursedItem-", StringComparison.Ordinal));
            if (cursed == null) continue;
            hero.AttachedItems.Remove(cursed);
            player.Hand.Add(cursed);
            return;
        }
    }

    private void PullFromHandWithBonus(Game game, Player from, Player to, bool heroBonus, bool magicBonus, bool challengeBonus)
    {
        if (from.Hand.Count == 0) return;
        var card = from.Hand[_rng.Next(from.Hand.Count)];
        from.Hand.Remove(card);
        to.Hand.Add(card);
        var def = CardCatalog.Get(card.DefinitionId);
        if (heroBonus && def.Type == CardType.Hero && from.Hand.Count > 0)
            StealRandomFromHand(game, from, to, 1);
        if (magicBonus && def.Type == CardType.Magic)
            Apply(game, to, def, from.Id);
        if (challengeBonus && def.Type == CardType.Challenge && from.Hand.Count > 0)
            StealRandomFromHand(game, from, to, 1);
    }

    private void StealRandomFromHand(Game game, Player from, Player to, int count)
    {
        for (var i = 0; i < count && from.Hand.Count > 0; i++)
        {
            var card = from.Hand[_rng.Next(from.Hand.Count)];
            from.Hand.Remove(card);
            to.Hand.Add(card);
        }
    }

    private void DestroyHeroOnOpponent(Game game, Player player, string? targetPlayerId)
    {
        var target = ResolveTarget(game, player, targetPlayerId);
        if (target != null) SacrificeRandomHero(game, target);
    }

    private bool StealHeroFromOpponent(Game game, Player player, string? targetPlayerId, out CardInstance? stolen)
    {
        stolen = null;
        var target = ResolveTarget(game, player, targetPlayerId);
        if (target == null || target.Party.Count == 0 || target.HeroTurnBuffs.HeroesCannotBeStolen)
            return false;

        var hero = target.Party[_rng.Next(target.Party.Count)];
        var heroClass = CardCatalog.Get(hero.DefinitionId).HeroClass;
        if (heroClass != null && PartyHasClass(player, heroClass.Value))
            return false;

        target.Party.Remove(hero);
        player.Party.Add(hero);
        stolen = hero;
        return true;
    }

    private bool StealHeroFromOpponent(Game game, Player player, string? targetPlayerId) =>
        StealHeroFromOpponent(game, player, targetPlayerId, out _);

    private void SacrificeRandomHero(Game game, Player player)
    {
        if (player.HeroTurnBuffs.HeroesCannotBeDestroyed || player.Party.Count == 0)
            return;

        var idx = _rng.Next(player.Party.Count);
        var hero = player.Party[idx];
        player.Party.RemoveAt(idx);
        foreach (var item in hero.AttachedItems)
            game.DiscardPile.Add(item);
        game.DiscardPile.Add(hero);
    }

    private void DestroyHeroOnOpponentWithItemReward(Game game, Player player, string? targetPlayerId)
    {
        var target = ResolveTarget(game, player, targetPlayerId);
        if (target == null || target.Party.Count == 0) return;

        var idx = _rng.Next(target.Party.Count);
        var hero = target.Party[idx];
        var items = hero.AttachedItems.ToList();
        target.Party.RemoveAt(idx);
        game.DiscardPile.Add(hero);
        var equip = items.FirstOrDefault(i => CardCatalog.Get(i.DefinitionId).Type == CardType.Item);
        if (equip != null)
            player.Hand.Add(equip);
        foreach (var other in items.Where(i => i != equip))
            game.DiscardPile.Add(other);
    }

    private void StealHeroSwapWithSource(Game game, Player player, string? targetPlayerId, CardInstance? sourceHero)
    {
        if (sourceHero == null || !player.Party.Contains(sourceHero)) return;
        var target = ResolveTarget(game, player, targetPlayerId);
        if (target == null || target.Party.Count == 0) return;

        var stolenIdx = _rng.Next(target.Party.Count);
        var stolen = target.Party[stolenIdx];
        target.Party.RemoveAt(stolenIdx);
        player.Party.Remove(sourceHero);
        target.Party.Add(sourceHero);
        player.Party.Add(stolen);
    }

    private void TryPlayMagicFromHand(Game game, Player player, string? targetPlayerId)
    {
        var magic = player.Hand.FirstOrDefault(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Magic);
        if (magic == null) return;
        var def = CardCatalog.Get(magic.DefinitionId);
        player.Hand.Remove(magic);
        game.DiscardPile.Add(magic);
        Apply(game, player, def, targetPlayerId);
    }

    private void TryPlayItemFromHand(Game game, Player player)
    {
        var item = player.Hand.FirstOrDefault(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Item);
        if (item == null || player.Party.Count == 0) return;
        var hero = player.Party[_rng.Next(player.Party.Count)];
        player.Hand.Remove(item);
        hero.AttachedItems.Add(item);
    }

    private void TryPlayHeroFromHand(Game game, Player player)
    {
        var hero = player.Hand.FirstOrDefault(c => CardCatalog.Get(c.DefinitionId).Type == CardType.Hero);
        if (hero == null) return;
        var def = CardCatalog.Get(hero.DefinitionId);
        if (def.HeroClass == null || PartyHasClass(player, def.HeroClass.Value)) return;
        player.Hand.Remove(hero);
        player.Party.Add(hero);
    }

    public void DestroyHeroOnOpponentPublic(Game game, Player player, string? targetPlayerId) =>
        DestroyHeroOnOpponent(game, player, targetPlayerId);

    public void TryPlayHeroFromHandPublic(Game game, Player player) =>
        TryPlayHeroFromHand(game, player);

    public void TryPlayItemFromHandPublic(Game game, Player player) =>
        TryPlayItemFromHand(game, player);

    private void RequestCardChoice(
        Game game,
        Player player,
        string prompt,
        IEnumerable<string> selectableIds,
        int min,
        int max,
        PendingContinuation continuation)
    {
        if (_choices == null)
            throw new InvalidOperationException("Choice system not available.");

        _choices.RequestSelectCards(game, player, prompt, selectableIds, min, max, continuation);
    }

    private void RequestOptionChoice(
        Game game,
        Player player,
        string prompt,
        IReadOnlyList<string> options,
        PendingContinuation continuation)
    {
        if (_choices == null)
            throw new InvalidOperationException("Choice system not available.");

        _choices.RequestSelectOption(game, player, prompt, options, continuation);
    }

    private void RequestPickFromDiscard(Game game, Player player, CardType type)
    {
        var matches = game.DiscardPile
            .Where(c => CardCatalog.Get(c.DefinitionId).Type == type)
            .Select(c => c.InstanceId)
            .ToList();

        if (matches.Count == 0) return;
        if (matches.Count == 1)
        {
            var card = game.DiscardPile.First(c => c.InstanceId == matches[0]);
            game.DiscardPile.Remove(card);
            player.Hand.Add(card);
            return;
        }

        RequestCardChoice(
            game,
            player,
            $"Choose a {type} card from the discard pile.",
            matches,
            1,
            1,
            new PendingContinuation
            {
                Type = PendingContinuationType.PickFromDiscard,
                DiscardTypeFilter = type
            });
    }

    private List<string> PullSpecificFromHand(Game game, Player from, Player to, int count)
    {
        var pulled = new List<string>();
        for (var i = 0; i < count && from.Hand.Count > 0; i++)
        {
            var card = from.Hand[_rng.Next(from.Hand.Count)];
            from.Hand.Remove(card);
            to.Hand.Add(card);
            pulled.Add(card.InstanceId);
        }

        return pulled;
    }

    private static List<string> ListCursedItemsOnParty(Player player)
    {
        var ids = new List<string>();
        foreach (var hero in player.Party)
        {
            foreach (var item in hero.AttachedItems)
            {
                if (CardCatalog.Get(item.DefinitionId).Id.StartsWith("CursedItem-", StringComparison.Ordinal))
                    ids.Add(item.InstanceId);
            }
        }

        return ids;
    }

    private static void ReturnCursedItemInstance(Player player, string itemInstanceId)
    {
        foreach (var hero in player.Party)
        {
            var item = hero.AttachedItems.FirstOrDefault(i => i.InstanceId == itemInstanceId);
            if (item == null) continue;
            hero.AttachedItems.Remove(item);
            player.Hand.Add(item);
            return;
        }
    }
}
