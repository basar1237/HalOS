# HalOS Web Konsol (Next.js 14) — çok aşamalı imaj, standalone çıktı (next.config output).
# Bağlam (context) web/console'dur (kendi kendine yeter: package.json + lock).
#   docker compose --profile apps up -d --build console
FROM node:20-alpine AS base

# --- Bağımlılıklar (reproducible: npm ci, lockfile gerekir) ---
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# --- Derleme (standalone Node sunucusu üretir) ---
FROM base AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
# NEXT_PUBLIC_* build-time inline edilir. Tarayıcı Gateway'e (host) localhost:5000'den konuşur;
# koddaki varsayılan bunu karşılar, ayrı ayar gerekmez.
RUN npm run build

# --- Çalışma (yalın runtime; yalnız standalone + static) ---
FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
# Standalone çıktı minimal server.js + gerekli node_modules'ı içerir.
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]
