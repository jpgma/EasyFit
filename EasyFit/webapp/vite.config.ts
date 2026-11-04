import { fileURLToPath } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const backend = "http://localhost:8080";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  server: {
    proxy: {
      // Mantém o Host do navegador, como o Caddy em produção. A API compara
      // Origin com Host para recusar escritas de outra origem (CSRF).
      "/api": { target: backend, changeOrigin: false },
      "/health": backend,
      "/ready": backend,
    },
  },
  build: {
    // O backend serve a SPA a partir do próprio wwwroot.
    outDir: "../wwwroot",
    emptyOutDir: true,
  },
});
