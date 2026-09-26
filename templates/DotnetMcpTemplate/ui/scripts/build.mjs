#!/usr/bin/env node
// Builds MCP App entries into ../src/DotnetMcpTemplate/ui_dist/<entry>.html.
//
//   pnpm run build                        all entries
//   pnpm run build projects-dashboard     only these entries (one or more names)
//   pnpm run watch [entry ...]            rebuild on save (use with `dotnet watch`)
//   pnpm run entries                      list entries
//
// An entry is a <name>.html + <name>.tsx pair in src/entries/. Names are lowercase kebab-case (they become
// ui://<name>); a leading "_" skips the entry, so you can stage work without shipping it.

import { readdir } from "node:fs/promises";
import { spawn } from "node:child_process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const entriesDir = join(root, "src", "entries");

const args = process.argv.slice(2);
const watch = args.includes("--watch");
const list = args.includes("--list");
const requested = args.filter((arg) => !arg.startsWith("--"));

async function discoverEntries() {
  const found = new Map();
  for (const file of await readdir(entriesDir)) {
    const match = file.match(/^([a-z0-9][a-z0-9-]*)\.(html|tsx)$/);
    if (!match || match[1] === "index") continue;
    found.set(match[1], (found.get(match[1]) ?? new Set()).add(match[2]));
  }
  return [...found].filter(([, exts]) => exts.has("html") && exts.has("tsx")).map(([name]) => name).sort();
}

function vite(entry) {
  return new Promise((resolvePromise, reject) => {
    const child = spawn("vite", ["build", ...(watch ? ["--watch"] : [])], {
      cwd: root,
      stdio: "inherit",
      shell: process.platform === "win32",
      env: { ...process.env, ENTRY: entry },
    });
    child.on("exit", (code) => (code === 0 ? resolvePromise() : reject(new Error(`vite build (${entry}) exited with ${code}`))));
  });
}

const available = await discoverEntries();
if (list) {
  console.log(available.join("\n"));
  process.exit(0);
}
if (available.length === 0) {
  console.error(`No entries in ${entriesDir}: add <name>.html + <name>.tsx.`);
  process.exit(1);
}

const unknown = requested.filter((name) => !available.includes(name));
if (unknown.length > 0) {
  console.error(`Unknown entr${unknown.length === 1 ? "y" : "ies"}: ${unknown.join(", ")}. Available: ${available.join(", ")}`);
  process.exit(1);
}

const entries = requested.length > 0 ? [...new Set(requested)] : available;
console.log(`${watch ? "Watching" : "Building"}: ${entries.join(", ")}`);

try {
  if (watch) {
    await Promise.all(entries.map(vite)); // one watcher per entry, runs until Ctrl+C
  } else {
    for (const entry of entries) await vite(entry);
    console.log(`Built ${entries.length} bundle(s) into src/DotnetMcpTemplate/ui_dist/`);
  }
} catch (error) {
  console.error(error.message);
  process.exit(1);
}
