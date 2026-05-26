import fs from "node:fs";
import path from "node:path";

const jsonPath = path.join(process.cwd(), "here-to-slay", "data", "base-deck-cards.json");
const doc = JSON.parse(fs.readFileSync(jsonPath, "utf8"));

/** Restored from user spreadsheet before normalize stripped partyReq fields. */
const PARTY = {
  "Monster-AbyssQueen": [{ genericHero: true, count: 2 }],
  "Monster-AnuranCauldron": [{ genericHero: true, count: 3 }],
  "Monster-ArcticAries": [{ genericHero: true, count: 1 }],
  "Monster-Bloodwing": [{ genericHero: true, count: 2 }],
  "Monster-CorruptedSabretooth": [{ genericHero: true, count: 3 }],
  "Monster-CrownedSerpent": [{ genericHero: true, count: 2 }],
  "Monster-DarkDragonKing": [{ heroClass: "Bard", count: 1 }, { genericHero: true, count: 1 }],
  "Monster-Dracos": [{ genericHero: true, count: 1 }],
  "Monster-Malamammoth": [{ heroClass: "Ranger", count: 1 }, { genericHero: true, count: 1 }],
  "Monster-MegaSlime": [{ genericHero: true, count: 4 }],
  "Monster-Orthus": [{ heroClass: "Wizard", count: 1 }, { genericHero: true, count: 1 }],
  "Monster-RexMajor": [{ heroClass: "Guardian", count: 1 }, { genericHero: true, count: 1 }],
  "Monster-Terratuga": [{ genericHero: true, count: 1 }],
  "Monster-TitanWyvern": [{ heroClass: "Fighter", count: 1 }, { genericHero: true, count: 1 }],
  "Monster-WarwornOwlbear": [{ heroClass: "Thief", count: 1 }, { genericHero: true, count: 1 }],
};

for (const card of doc.cards) {
  if (PARTY[card.id]) card.partyRequirements = PARTY[card.id];
}

fs.writeFileSync(jsonPath, JSON.stringify(doc, null, 2) + "\n", "utf8");
console.log("Updated monster party requirements.");
