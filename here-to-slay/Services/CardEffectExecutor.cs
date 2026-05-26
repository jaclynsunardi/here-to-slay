using HereToSlay.Models;

namespace HereToSlay.Services;

/// <summary>Resolves card effects from effectText / effectScript using keyword rules.</summary>
public class CardEffectExecutor
{
    private readonly Random _rng;

    public CardEffectExecutor(Random rng) => _rng = rng;

    public void Apply(Game game, Player player, CardDefinition def, string? targetPlayerId = null, CardInstance? sourceHero = null)
    {
        var script = string.IsNullOrWhiteSpace(def.EffectScript) ? def.EffectText : def.EffectScript;
        if (string.IsNullOrWhiteSpace(script)) return;

        var t = script.ToUpperInvariant();
        var target = ResolveTarget(game, player, targetPlayerId);

        if (t.Contains("TRADE HANDS"))
        {
            if (target != null) TradeHands(player, target);
            return;
        }

        if (t.Contains("LOOK AT THE TOP 3"))
        {
            ScryThree(game, player);
            return;
        }

        if (t.Contains("DRAW") && t.Contains("UNTIL") && t.Contains("7"))
        {
            DrawUntilHandSize(game, player, 7);
            return;
        }

        if (t.Contains("DRAW 3") && t.Contains("DISCARD"))
        {
            DrawCards(game, player, 3);
            DiscardFromHand(game, player, 1, playerChooses: false);
            return;
        }

        if (t.Contains("DRAW 2") && t.Contains("DISCARD 2"))
        {
            DrawCards(game, player, 2);
            DiscardFromHand(game, player, 2, playerChooses: false);
            return;
        }

        if (t.Contains("DRAW 2"))
        {
            DrawCards(game, player, 2);
            return;
        }

        if (t.Contains("DRAW A CARD") || t.Contains("DRAW 1") || (t.StartsWith("DRAW") && !t.Contains("DISCARD") && !t.Contains("MAGIC")))
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

        if (t.Contains("EACH OTHER PLAYER") && t.Contains("DISCARD"))
        {
            var piles = new List<CardInstance>();
            foreach (var p in game.Players.Where(p => p.Id != player.Id))
                piles.AddRange(DiscardFromHand(game, p, 1, playerChooses: false));
            if (piles.Count > 0)
            {
                var pick = piles[_rng.Next(piles.Count)];
                player.Hand.Add(pick);
            }
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
            if (target != null) DiscardFromHand(game, target, 2, playerChooses: false);
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

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("MAGIC"))
        {
            TakeFromDiscard(game, player, CardType.Magic);
            return;
        }

        if (t.Contains("SEARCH THE DISCARD") && t.Contains("HERO"))
        {
            TakeFromDiscard(game, player, CardType.Hero);
            return;
        }

        if (t.Contains("RETURN A CURSED ITEM"))
        {
            ReturnCursedItemToHand(player);
            return;
        }

        if (t.Contains("PULL A CARD") || t.Contains("STEAL") && t.Contains("HAND"))
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

        if (t.Contains("STEAL A HERO") && t.Contains("DESTROY A HERO"))
        {
            StealHeroFromOpponent(game, player, targetPlayerId);
            DestroyHeroOnOpponent(game, player, targetPlayerId);
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

        if (t.Contains("PLAY AN ITEM") && t.Contains("DRAW"))
        {
            DrawCards(game, player, 1);
            return;
        }

        if (t.Contains("PLAY A MAGIC") || t.Contains("PLAY IT IMMEDIATELY"))
        {
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
        if (target == null || target.Party.Count == 0) return false;

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
        if (player.Party.Count == 0) return;
        var idx = _rng.Next(player.Party.Count);
        var hero = player.Party[idx];
        player.Party.RemoveAt(idx);
        foreach (var item in hero.AttachedItems)
            game.DiscardPile.Add(item);
        game.DiscardPile.Add(hero);
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
}
