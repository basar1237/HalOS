import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// Tauri masaüstü frontend'i (ADR-005). Tauri sabit bir port bekler; sunucu 1420'de çalışır.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  clearScreen: false,
  server: { port: 1420, strictPort: true },
  build: { outDir: 'dist', target: 'es2020' },
  test: { environment: 'jsdom', include: ['src/**/*.test.ts'] },
});
