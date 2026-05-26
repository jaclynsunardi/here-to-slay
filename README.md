# Here to Slay (Digital)

Online **Here to Slay** using the official core rules: **3 action points per turn**, party leaders, monster deck, party requirements, fight-back ranges, challenges, hero rolls, items, and magic.

## Run locally

**Backend:**
```bash
cd here-to-slay
dotnet run
```

**Frontend:**
```bash
cd here-to-slay-client
npm install
npm run dev
```

Open `http://localhost:5173` — restart the API after pulling rule changes (`Ctrl+C`, then `dotnet run` again).

## Official rules (implemented)

| Cost | Action |
|------|--------|
| 1 AP | Draw a card |
| 1 AP | Play Hero, Item, or Magic |
| 1 AP | Roll a hero in your party for its ability (once per hero per turn) |
| 1 AP | Play Challenge on an opponent (blocks their next Hero/Item unless they win the roll-off) |
| 2 AP | Attack a monster (party requirements + roll ≥ threshold; natural roll may trigger fight-back) |
| 3 AP | Discard your entire hand and draw 5 |

**Win:** Slay 3 monsters, or end your turn with **6 different classes** in your party (including your party leader).

## Notes

- Game state is in-memory (resets when the API restarts).
- Card text is simplified to match available art; effects follow the base-deck patterns from the rulebook.
- Expansions are not yet in the deck builder.

## Card art and data

Renamed base-deck images live under `here-to-slay-client/public/images/Game/Base Deck/_organized/`. The server loads **`here-to-slay/data/base-deck-cards.json`** at startup (full base deck, copies, monster slay/fail thresholds, party requirements).

Monster attacks (official rules): roll ≥ `rollThreshold` to slay; roll ≤ `failIfBelowOrEqual` triggers `failPenalty` (`SacrificeHero` or `Discard2`); between those values, nothing happens. Party requirement `Hero` = generic hero cards in party (party leader does not count).

After editing JSON, restart the API. Optional: `node tools/normalize-base-deck-json.mjs` if you add new `"if below N-, ..."` keys on monsters.
