import fs from "node:fs";
import path from "node:path";

const jsonPath = path.join(process.cwd(), "here-to-slay", "data", "base-deck-cards.json");
const doc = JSON.parse(fs.readFileSync(jsonPath, "utf8"));

function parseFailKey(key) {
  const m = key.match(/if\s+below\s*(\d+)\s*-/i);
  if (!m) return null;
  const n = Number(m[1]);
  let penalty = "None";
  const upper = key.toUpperCase();
  if (upper.includes("SACRIFICE") && upper.includes("HERO")) penalty = "SacrificeHero";
  else if (upper.includes("DISCARD") && upper.includes("2")) penalty = "Discard2";
  return { failIfBelowOrEqual: n, failPenalty: penalty };
}

for (const card of doc.cards) {
  if (card.type !== "Monster") continue;

  for (const key of Object.keys(card)) {
    if (!key.toLowerCase().startsWith("if below")) continue;
    const parsed = parseFailKey(key);
    if (parsed) {
      card.failIfBelowOrEqual = parsed.failIfBelowOrEqual;
      card.failPenalty = parsed.failPenalty;
    }
    delete card[key];
  }

  if (!card.rollThreshold || card.rollThreshold === 0) {
    const slay = Number(card.fightBackMin);
    if (slay >= 4 && slay <= 18) card.rollThreshold = slay;
  }

  delete card.fightBackMin;
  delete card.fightBackMax;

  const reqs = [];
  for (let i = 1; i <= 4; i++) {
    const cls = card[`partyReq${i}Class`];
    const count = card[`partyReq${i}Count`];
    delete card[`partyReq${i}Class`];
    delete card[`partyReq${i}Count`];
    if (!cls || cls === "") continue;
    const c = count > 0 ? count : 1;
    if (cls === "Hero") reqs.push({ genericHero: true, count: c });
    else reqs.push({ heroClass: cls, count: c });
  }
  card.partyRequirements = reqs;
}

fs.writeFileSync(jsonPath, JSON.stringify(doc, null, 2) + "\n", "utf8");
console.log(`Normalized ${doc.cards.filter((c) => c.type === "Monster").length} monsters in ${jsonPath}`);
