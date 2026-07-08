import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

// Frontend birim testleri (saf mantık: api-client hata çıkarımı, etiket haritaları, yardımcılar).
// jsdom ortamı localStorage/window sağlar (token-storage, api-client). Alias @/* → ./src/*
// tsconfig ile aynı.
export default defineConfig({
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.ts'],
    globals: true,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
});
