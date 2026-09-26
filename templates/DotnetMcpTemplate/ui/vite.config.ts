import { resolve } from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { viteSingleFile } from "vite-plugin-singlefile";

// One self-contained HTML file per entry (MCP hosts load a single document into a sandboxed iframe).
// Build one entry at a time: ENTRY=projects-dashboard pnpm run build  (default: projects-dashboard)
const entry = process.env.ENTRY ?? "projects-dashboard";

export default defineConfig({
  plugins: [react(), viteSingleFile()],
  root: resolve(__dirname, "src", entry),
  build: {
    outDir: resolve(__dirname, "../src/DotnetMcpTemplate/ui_dist"),
    emptyOutDir: false,
    rollupOptions: {
      input: resolve(__dirname, "src", entry, `${entry}.html`),
    },
  },
});
