/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Docker imajı için bağımsız (standalone) çıktı: .next/standalone içinde minimal Node sunucusu
  // + yalnız gerekli node_modules üretir (yalın imaj). Bkz. deploy/docker-compose.yml console servisi.
  output: 'standalone',
};

export default nextConfig;
