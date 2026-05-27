/**
 * Sets heroEffect / magicEffect enum categories from effectText.
 * Does not change effectText, heroClass, or type.
 */
import fs from "node:fs";
import path from "node:path";

const jsonPath = path.join(process.cwd(), "here-to-slay", "data", "base-deck-cards.json");

const HERO_EFFECTS = new Set([
  "None",
  "Draw1",
  "Draw2",
  "OpponentDiscard1",
  "StealRandomFromHand",
  "DestroyOpponentHero",
  "SacrificeOwnHeroDraw2",
  "SearchDeckDrawHero",
  "AllPlayersDiscard1",
]);

const MAGIC_EFFECTS = new Set([
  "None",
  "Draw2",
  "Draw3Discard2",
  "DestroyOpponentHero",
  "StealOpponentHero",
  "AllDraw1",
  "ReviveFromDiscard",
]);

function stripRollPrefix(effectText) {
  return (effectText ?? "").replace(/^Roll\s+\d+\+?:\s*/i, "").trim();
}

function categorizeHero(effectText) {
  const t = stripRollPrefix(effectText).toUpperCase();

  if (!t || t.includes("DO NOTHING")) return "None";
  if (/^DRAW A CARD\b/.test(t) && !t.includes(" AND ") && !t.includes(" UNTIL ")) return "Draw1";
  if (/^DRAW 2 CARDS?\./.test(t) || (t.startsWith("DRAW 2 CARD") && !t.includes(" IF ")))
    return "Draw2";
  if (t.includes("EACH OTHER PLAYER") && t.includes("DISCARD") && t.includes("CHOOSE"))
    return "AllPlayersDiscard1";
  if (t.includes("CHOOSE A PLAYER") && t.includes("DISCARD 2")) return "OpponentDiscard1";
  if (
    (t.includes("PULL") || t.includes("GIVE YOU") || t.includes("FROM THEIR HAND")) &&
    !t.includes("STEAL A HERO") &&
    !t.includes("PARTY")
  ) {
    return "StealRandomFromHand";
  }
  if (t.includes("DESTROY A HERO") && !t.includes("STEAL") && !t.includes("DESTROY 2"))
    return "DestroyOpponentHero";
  if (t.includes("SACRIFICE") && t.includes("DRAW 2")) return "SacrificeOwnHeroDraw2";
  if (t.includes("SEARCH") && t.includes("DISCARD") && t.includes("HERO CARD"))
    return "SearchDeckDrawHero";

  return "None";
}

function categorizeMagic(effectText) {
  const t = (effectText ?? "").toUpperCase();

  if (t.includes("DRAW 3") && t.includes("DISCARD")) return "Draw3Discard2";
  if (t === "DRAW 2 CARDS." || /^DRAW 2 CARDS?\.?$/.test(t)) return "Draw2";
  if (t.includes("ALL PLAYERS") && t.includes("DRAW")) return "AllDraw1";
  if (t.includes("SEARCH") && t.includes("DISCARD") && t.includes("HERO"))
    return "ReviveFromDiscard";
  if (t.includes("DISCARD") && t.includes("DESTROY") && t.includes("HERO"))
    return "DestroyOpponentHero";
  if (t.includes("STEAL") && t.includes("HERO")) return "StealOpponentHero";

  return "None";
}

function categorizeCard(card) {
  const prefix = card.id.split("-")[0];

  if (card.type === "PartyLeader") {
    card.heroEffect = "None";
    card.magicEffect = "None";
    card.modifierBonus = 0;
    delete card.modifierBonusAlt;
    return;
  }

  if (card.type === "Hero") {
    const cat = categorizeHero(card.effectText);
    card.heroEffect = cat;
    card.magicEffect = "None";
    card.modifierBonus = 0;
    delete card.modifierBonusAlt;
    return;
  }

  if (card.type === "Magic") {
    card.magicEffect = categorizeMagic(card.effectText);
    delete card.heroEffect;
    delete card.heroEffectMinRoll;
    delete card.modifierBonus;
    delete card.modifierBonusAlt;
    return;
  }

  if (card.type === "Item") {
    card.itemKind = prefix === "CursedItem" ? "Cursed" : "Equipment";
    delete card.heroEffect;
    delete card.heroEffectMinRoll;
    delete card.magicEffect;
    delete card.modifierBonus;
    delete card.modifierBonusAlt;
    return;
  }

  if (card.type === "Modifier") {
    delete card.heroEffect;
    delete card.heroEffectMinRoll;
    delete card.magicEffect;
    return;
  }

  if (card.type === "Challenge" || card.type === "Monster") {
    delete card.heroEffect;
    delete card.heroEffectMinRoll;
    delete card.magicEffect;
    delete card.modifierBonus;
    delete card.modifierBonusAlt;
  }
}

const doc = JSON.parse(fs.readFileSync(jsonPath, "utf8"));

doc.instructions = [
  "Copy the shape of `example` for each card in `cards`.",
  "effectText: full rules text shown in the UI (source of truth for effect resolution).",
  "heroClass: Bard | Ranger | Thief | Wizard | Guardian | Fighter",
  "heroEffect (Hero / PartyLeader): None | Draw1 | Draw2 | OpponentDiscard1 | StealRandomFromHand | DestroyOpponentHero | SacrificeOwnHeroDraw2 | SearchDeckDrawHero | AllPlayersDiscard1",
  "magicEffect (Magic): None | Draw2 | Draw3Discard2 | DestroyOpponentHero | StealOpponentHero | AllDraw1 | ReviveFromDiscard",
  "itemKind (Item): Equipment | Cursed",
  "Monsters: use partyRequirements array or partyReq1Class / partyReq1Count fields.",
].join("\n");

for (const card of doc.cards) {
  categorizeCard(card);
}

fs.writeFileSync(jsonPath, JSON.stringify(doc, null, 2) + "\n", "utf8");

const heroes = doc.cards.filter((c) => c.type === "Hero");
const byEffect = Object.groupBy(heroes, (c) => c.heroEffect);
console.log("Hero categories:");
for (const [k, v] of Object.entries(byEffect).sort()) {
  console.log(`  ${k}: ${v.length}`);
}
console.log("Wrote", doc.cards.length, "cards");
