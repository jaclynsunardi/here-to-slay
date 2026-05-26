# Base deck card data

Edit **`base-deck-cards.json`** — one object per card in the `cards` array.

At the top of the file, **`example`** shows a fully filled hero (Beary Wise). Every hero in `cards` uses the same fields; magic/modifier/monster/item rows only include the fields they need.

Regenerate stubs after adding or renaming art:

```bash
node tools/generate-card-manifest.mjs
```

(Warning: regenerating overwrites your edits. Commit or back up first.)

## Monster party requirements

Use flat fields on each monster row:

- `partyReq1Class` / `partyReq1Count` — required
- `partyReq2Class` / `partyReq2Count` — optional second requirement
- Class values: `Bard`, `Ranger`, `Thief`, `Wizard`, `Guardian`, `Fighter`, or `Any`

## When the file is ready

Tell us (or open a PR) and we’ll load this JSON in `CardCatalog` on the server.
