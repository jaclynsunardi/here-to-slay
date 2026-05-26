import fs from "node:fs";
import path from "node:path";

const organizedDir = path.join(
  process.cwd(),
  "here-to-slay-client",
  "public",
  "images",
  "Game",
  "Base Deck",
  "_organized"
);

const outPath = path.join(process.cwd(), "here-to-slay", "data", "base-deck-cards.json");

const PREFIX_TO_TYPE = {
  Hero: "Hero",
  Item: "Item",
  CursedItem: "Item",
  Magic: "Magic",
  Modifier: "Modifier",
  Challenge: "Challenge",
  Monster: "Monster",
  PartyLeader: "PartyLeader",
};

const PREFIXES = Object.keys(PREFIX_TO_TYPE).sort(
  (a, b) => b.length - a.length
);

const EXAMPLE = {
  id: "Hero-BearyWise",
  name: "Beary Wise",
  type: "Hero",
  heroClass: "Guardian",
  heroEffect: "Draw1",
  heroEffectMinRoll: 6,
  magicEffect: "None",
  modifierBonus: 0,
  copies: 2,
  effectText: "Roll 6+: Draw 1 card.",
};

const INSTRUCTIONS = [
  "Copy the shape of `example` for each card in `cards`.",
  "Leave effectText as \"\" until you add rules text.",
  "heroClass: Bard | Ranger | Thief | Wizard | Guardian | Fighter",
  "heroEffect: None | Draw1 | Draw2 | OpponentDiscard1 | StealRandomFromHand | DestroyOpponentHero | SacrificeOwnHeroDraw2 | SearchDeckDrawHero | AllPlayersDiscard1",
  "magicEffect: None | Draw2 | Draw3Discard2 | DestroyOpponentHero | StealOpponentHero | AllDraw1 | ReviveFromDiscard",
  "Monsters: set partyReq1Class to Any if any class counts; leave partyReq2* blank or delete those keys.",
].join("\n");

function parsePrefix(filename) {
  const base = filename.replace(/\.png$/i, "");
  for (const prefix of PREFIXES) {
    if (base === prefix || base.startsWith(`${prefix}-`)) {
      return prefix;
    }
  }
  return null;
}

function slugToName(slug) {
  return slug
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/([A-Za-z])(\d)/g, "$1 $2")
    .trim();
}

function collectPngFiles(dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      out.push(...collectPngFiles(full));
    } else if (entry.name.toLowerCase().endsWith(".png")) {
      out.push(entry.name);
    }
  }
  return out;
}

function stubCard(type, id, name) {
  switch (type) {
    case "Hero":
    case "PartyLeader":
      return {
        id,
        name,
        type,
        heroClass: "",
        heroEffect: "None",
        heroEffectMinRoll: 6,
        magicEffect: "None",
        modifierBonus: 0,
        copies: type === "PartyLeader" ? 1 : 2,
        effectText: "",
      };
    case "Magic":
      return {
        id,
        name,
        type,
        magicEffect: "None",
        copies: 2,
        effectText: "",
      };
    case "Modifier":
      return {
        id,
        name,
        type,
        modifierBonus: 1,
        copies: 1,
        effectText: "",
      };
    case "Monster":
      return {
        id,
        name,
        type,
        rollThreshold: 0,
        fightBackMin: 2,
        fightBackMax: 5,
        partyReq1Class: "",
        partyReq1Count: 0,
        partyReq2Class: "",
        partyReq2Count: 0,
        copies: 1,
        effectText: "",
      };
    case "Item":
    case "Challenge":
    default:
      return {
        id,
        name,
        type,
        copies: 1,
        effectText: "",
      };
  }
}

function typeSortOrder(type) {
  const order = [
    "PartyLeader",
    "Hero",
    "Item",
    "Magic",
    "Modifier",
    "Challenge",
    "Monster",
  ];
  return order.indexOf(type);
}

function main() {
  const files = collectPngFiles(organizedDir).sort((a, b) =>
    a.localeCompare(b)
  );

  const cards = files
    .map((file) => {
      const base = file.replace(/\.png$/i, "");
      const prefix = parsePrefix(file);
      const type = PREFIX_TO_TYPE[prefix] ?? "Item";
      const namePart = prefix ? base.slice(prefix.length + 1) : base;
      const name = slugToName(namePart || base);
      return stubCard(type, base, name);
    })
    .sort((a, b) => {
      const t = typeSortOrder(a.type) - typeSortOrder(b.type);
      return t !== 0 ? t : a.id.localeCompare(b.id);
    });

  const doc = {
    version: 1,
    instructions: INSTRUCTIONS,
    example: EXAMPLE,
    cards,
  };

  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, JSON.stringify(doc, null, 2) + "\n", "utf8");

  console.log(`Wrote ${cards.length} card stub(s) + example to ${outPath}`);
}

main();
