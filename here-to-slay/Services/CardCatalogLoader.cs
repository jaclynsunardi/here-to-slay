using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HereToSlay.Models;

namespace HereToSlay.Services;

public static class CardCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyDictionary<string, CardDefinition> Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "data", "base-deck-cards.json");
        if (!File.Exists(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), "data", "base-deck-cards.json");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Card data not found: {path}");

        var doc = JsonSerializer.Deserialize<CardDataFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse base-deck-cards.json");

        var map = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        foreach (var raw in doc.Cards)
        {
            var def = ToDefinition(raw);
            map[def.Id] = def;
        }

        return map;
    }

    private static CardDefinition ToDefinition(RawCard raw)
    {
        var type = ParseEnum<CardType>(raw.Type);
        HeroClass? heroClass = string.IsNullOrWhiteSpace(raw.HeroClass)
            ? null
            : ParseEnum<HeroClass>(raw.HeroClass);

        var effectScript = type switch
        {
            CardType.Hero or CardType.PartyLeader or CardType.Magic => StripRollPrefix(raw.EffectText),
            _ => ""
        };

        var (modBonus, modAlt) = ParseModifierBonus(raw.ModifierBonus);
        modAlt ??= raw.ModifierBonusAlt;

        var def = new CardDefinition
        {
            Id = raw.Id,
            Name = raw.Name,
            Type = type,
            HeroClass = heroClass,
            HeroEffect = ParseHeroEffect(raw.HeroEffect),
            MagicEffect = ParseMagicEffect(raw.MagicEffect),
            ItemKind = ParseItemKind(raw.ItemKind),
            PartyLeaderAbility = ParsePartyLeaderAbility(raw.PartyLeaderAbility),
            PartyLeaderAbilityValue = raw.PartyLeaderAbilityValue ?? 0,
            PartyLeaderAbilityAltValue = raw.PartyLeaderAbilityAltValue,
            HeroEffectMinRoll = raw.HeroEffectMinRoll ?? 6,
            EffectText = raw.EffectText ?? "",
            EffectScript = effectScript.Trim(),
            ModifierBonus = modBonus,
            ModifierBonusAlt = modAlt,
            Copies = raw.Copies ?? DefaultCopies(type),
            ImagePath = CardCatalog.GetOrganizedImagePath(raw.Id, type),
            RollThreshold = raw.RollThreshold ?? 0,
            FailIfRollAtOrBelow = raw.FailIfBelowOrEqual,
            FailPenalty = ParseFailPenalty(raw.FailPenalty),
            PartyRequirements = ParsePartyRequirements(raw)
        };

        if (type == CardType.Monster && def.RollThreshold <= 0)
            def.RollThreshold = 8;

        return def;
    }

    private static List<PartyRequirement> ParsePartyRequirements(RawCard raw)
    {
        var list = new List<PartyRequirement>();
        if (raw.PartyRequirements is { Count: > 0 })
        {
            foreach (var r in raw.PartyRequirements)
            {
                if (r.GenericHero)
                    list.Add(new PartyRequirement(null, r.Count, true));
                else if (!string.IsNullOrWhiteSpace(r.HeroClass))
                    list.Add(new PartyRequirement(ParseEnum<HeroClass>(r.HeroClass), r.Count));
            }

            return list;
        }

        for (var i = 1; i <= 4; i++)
        {
            var cls = GetPartyReqClass(raw, i);
            var count = GetPartyReqCount(raw, i);
            if (string.IsNullOrWhiteSpace(cls)) continue;
            var c = count > 0 ? count : 1;
            if (cls.Equals("Hero", StringComparison.OrdinalIgnoreCase))
                list.Add(new PartyRequirement(null, c, true));
            else
                list.Add(new PartyRequirement(ParseEnum<HeroClass>(cls), c));
        }

        return list;
    }

    private static string? GetPartyReqClass(RawCard raw, int index) => index switch
    {
        1 => raw.PartyReq1Class,
        2 => raw.PartyReq2Class,
        3 => raw.PartyReq3Class,
        4 => raw.PartyReq4Class,
        _ => null
    };

    private static int GetPartyReqCount(RawCard raw, int index) => index switch
    {
        1 => raw.PartyReq1Count ?? 0,
        2 => raw.PartyReq2Count ?? 0,
        3 => raw.PartyReq3Count ?? 0,
        4 => raw.PartyReq4Count ?? 0,
        _ => 0
    };

    private static AttackFailPenalty ParseFailPenalty(string? value) =>
        value?.Trim() switch
        {
            "SacrificeHero" => AttackFailPenalty.SacrificeHero,
            "Discard2" => AttackFailPenalty.Discard2,
            _ => AttackFailPenalty.None
        };

    private static int DefaultCopies(CardType type) => type switch
    {
        CardType.Hero => 2,
        CardType.Item or CardType.Magic => 2,
        CardType.Modifier or CardType.Challenge => 1,
        _ => 1
    };

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value.Trim(), ignoreCase: true);

    private static (int bonus, int? alt) ParseModifierBonus(JsonNode? node)
    {
        if (node == null)
            return (0, null);

        if (node is JsonValue val)
        {
            if (val.TryGetValue<int>(out var n))
                return (n, null);
            if (val.TryGetValue<double>(out var d))
                return ((int)d, null);
            if (val.TryGetValue<string>(out var s))
                return ParseModifierBonusString(s);
        }

        return (0, null);
    }

    private static (int bonus, int? alt) ParseModifierBonusString(string raw)
    {
        var s = raw.Trim();
        if (string.IsNullOrEmpty(s))
            return (0, null);

        if (s.Contains('/'))
        {
            var parts = s.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (ParseSignedInt(parts[0]), ParseSignedInt(parts[1]));
        }

        return (ParseSignedInt(s), null);
    }

    private static int ParseSignedInt(string s)
    {
        s = s.Trim().TrimStart('+');
        return int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string StripRollPrefix(string? effectText)
    {
        if (string.IsNullOrWhiteSpace(effectText))
            return "";

        return RollPrefixRegex.Replace(effectText.Trim(), "").Trim();
    }

    private static readonly Regex RollPrefixRegex = new(
        @"^Roll\s+\d+\+?:\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static HeroEffect ParseHeroEffect(string? value) =>
        Enum.TryParse<HeroEffect>(value?.Trim(), true, out var effect) ? effect : HeroEffect.None;

    private static MagicEffect ParseMagicEffect(string? value) =>
        Enum.TryParse<MagicEffect>(value?.Trim(), true, out var effect) ? effect : MagicEffect.None;

    private static ItemKind? ParseItemKind(string? value) =>
        Enum.TryParse<ItemKind>(value?.Trim(), true, out var kind) ? kind : null;

    private static PartyLeaderAbilityKind ParsePartyLeaderAbility(string? value) =>
        Enum.TryParse<PartyLeaderAbilityKind>(value?.Trim(), true, out var kind)
            ? kind
            : PartyLeaderAbilityKind.None;

    private sealed class CardDataFile
    {
        public List<RawCard> Cards { get; set; } = [];
    }

    private sealed class RawCard
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string? HeroClass { get; set; }
        public string? HeroEffect { get; set; }
        public int? HeroEffectMinRoll { get; set; }
        public string? MagicEffect { get; set; }
        public string? ItemKind { get; set; }
        public string? PartyLeaderAbility { get; set; }
        public int? PartyLeaderAbilityValue { get; set; }
        public int? PartyLeaderAbilityAltValue { get; set; }
        public JsonNode? ModifierBonus { get; set; }
        public int? ModifierBonusAlt { get; set; }
        public int? Copies { get; set; }
        public string? EffectText { get; set; }
        public int? RollThreshold { get; set; }
        public int? FailIfBelowOrEqual { get; set; }
        public string? FailPenalty { get; set; }
        public string? PartyReq1Class { get; set; }
        public int? PartyReq1Count { get; set; }
        public string? PartyReq2Class { get; set; }
        public int? PartyReq2Count { get; set; }
        public string? PartyReq3Class { get; set; }
        public int? PartyReq3Count { get; set; }
        public string? PartyReq4Class { get; set; }
        public int? PartyReq4Count { get; set; }
        public List<RawPartyReq>? PartyRequirements { get; set; }
    }

    private sealed class RawPartyReq
    {
        public string? HeroClass { get; set; }
        public bool GenericHero { get; set; }
        public int Count { get; set; }
    }
}
