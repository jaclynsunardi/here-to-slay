using HereToSlay.Models;

namespace HereToSlay.Services;

public static class CardCatalog
{
    private const string OrganizedBasePath = "/images/Game/Base Deck/_organized";

    public static readonly IReadOnlyDictionary<string, CardDefinition> Definitions =
        CardCatalogLoader.Load();

    public static string[] PartyLeaderIds { get; } = Definitions.Values
        .Where(c => c.Type == CardType.PartyLeader)
        .OrderBy(c => c.Id, StringComparer.Ordinal)
        .Select(c => c.Id)
        .ToArray();

    public static CardDefinition Get(string definitionId) =>
        Definitions.TryGetValue(definitionId, out var def)
            ? def
            : throw new KeyNotFoundException($"Unknown card: {definitionId}");

    public static string GetOrganizedImagePath(string imageFileName, CardType type)
    {
        var folder = type switch
        {
            CardType.Hero => "heroes",
            CardType.Item => "items",
            CardType.Magic => "magic",
            CardType.Modifier => "modifiers",
            CardType.Challenge => "challenges",
            CardType.Monster => "monsters",
            CardType.PartyLeader => "party-leaders",
            _ => "other"
        };

        return $"{OrganizedBasePath}/{folder}/{imageFileName}.png";
    }

    public static List<CardInstance> BuildMainDeck(Random rng)
    {
        var deck = new List<CardInstance>();

        foreach (var def in Definitions.Values)
        {
            if (def.Type is CardType.Monster or CardType.PartyLeader)
                continue;

            for (var i = 0; i < def.Copies; i++)
                deck.Add(new CardInstance { DefinitionId = def.Id });
        }

        Shuffle(deck, rng);
        return deck;
    }

    public static List<CardInstance> BuildMonsterDeck(Random rng)
    {
        var deck = new List<CardInstance>();
        foreach (var def in Definitions.Values.Where(c => c.Type == CardType.Monster))
        {
            for (var i = 0; i < def.Copies; i++)
                deck.Add(new CardInstance { DefinitionId = def.Id });
        }

        Shuffle(deck, rng);
        return deck;
    }

    private static void Shuffle(List<CardInstance> deck, Random rng)
    {
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }
}
