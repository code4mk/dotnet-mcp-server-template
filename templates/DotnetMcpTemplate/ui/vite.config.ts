import { resolve } from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { viteSingleFile } from "vite-plugin-singlefile";

// Each MCP App is one self-contained HTML file (hosts load a single document into a sandboxed iframe).
// vite-plugin-singlefile inlines everything, which needs one input per build, so scripts/build.mjs runs
// `vite build` once per entry with ENTRY=<name>. Output: ../src/DotnetMcpTemplate/ui_dist/<entry>.html,
// served by the server as ui://<entry>.
//
// `pnpm dev` (no ENTRY) serves src/entries/index.html: a sandbox linking every entry with sample data.
export default defineConfig(({ command }) => {
  const entry = process.env.ENTRY;
  if (command === "build" && !entry) {
    throw new Error("ENTRY is required for vite build. Run `pnpm run build`, which builds every entry.");
  }

  return {
    root: resolve(import.meta.dirname, "src/entries"),
    plugins: [react(), tailwindcss(), viteSingleFile()],
    resolve: { alias: { "@": resolve(import.meta.dirname, "src") } },
    build: {
      outDir: resolve(import.meta.dirname, "../src/DotnetMcpTemplate/ui_dist"),
      emptyOutDir: false, // other entries live there too
      rollupOptions: { input: entry ? resolve(import.meta.dirname, `src/entries/${entry}.html`) : undefined },
    },
  };
});
