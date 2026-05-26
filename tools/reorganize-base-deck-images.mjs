import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(process.cwd());
const organizedDir = path.join(
  repoRoot,
  "here-to-slay-client",
  "public",
  "images",
  "Game",
  "Base Deck",
  "_organized"
);

/** Folder name for each filename prefix (case-sensitive). */
const PREFIX_TO_FOLDER = {
  Hero: "heroes",
  Item: "items",
  CursedItem: "items",
  Magic: "magic",
  Modifier: "modifiers",
  Challenge: "challenges",
  Monster: "monsters",
  PartyLeader: "party-leaders",
};

const PREFIXES = Object.keys(PREFIX_TO_FOLDER).sort(
  (a, b) => b.length - a.length
);

function folderForFilename(filename) {
  const base = filename.replace(/\.png$/i, "");
  for (const prefix of PREFIXES) {
    if (base === prefix || base.startsWith(`${prefix}-`)) {
      return PREFIX_TO_FOLDER[prefix];
    }
  }
  return "other";
}

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

function collectPngFiles(dir) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      out.push(...collectPngFiles(full));
    } else if (entry.name.toLowerCase().endsWith(".png")) {
      out.push(full);
    }
  }
  return out;
}

function main() {
  if (!fs.existsSync(organizedDir)) {
    throw new Error(`Organized folder not found: ${organizedDir}`);
  }

  const files = collectPngFiles(organizedDir);
  const byFolder = new Map();

  for (const src of files) {
    const name = path.basename(src);
    const folder = folderForFilename(name);
    const dest = path.join(organizedDir, folder, name);

    if (path.resolve(src) === path.resolve(dest)) {
      byFolder.set(folder, (byFolder.get(folder) ?? 0) + 1);
      continue;
    }

    ensureDir(path.dirname(dest));
    if (fs.existsSync(dest)) {
      throw new Error(`Duplicate target: ${dest}`);
    }
    fs.renameSync(src, dest);
    byFolder.set(folder, (byFolder.get(folder) ?? 0) + 1);
  }

  // Remove empty subfolders left behind (e.g. old "reference").
  for (const entry of fs.readdirSync(organizedDir, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const sub = path.join(organizedDir, entry.name);
    const remaining = fs.readdirSync(sub);
    if (remaining.length === 0) {
      fs.rmdirSync(sub);
    }
  }

  console.log(`Reorganized ${files.length} image(s) under: ${organizedDir}`);
  for (const [folder, count] of [...byFolder.entries()].sort()) {
    console.log(`  ${folder}: ${count}`);
  }
}

main();
