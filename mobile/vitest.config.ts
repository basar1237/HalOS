import { defineConfig } from 'vitest/config';

// Saf mantık birim testleri (RN çalışma zamanı GEREKMEZ): biçimlendirme yardımcıları vb.
// RN/expo modüllerine dokunan dosyalar (token/api) cihaz/mocks gerektirdiğinden burada test edilmez.
export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
});
