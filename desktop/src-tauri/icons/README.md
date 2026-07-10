# Uygulama ikonları

Bu klasördeki ikon dosyaları (`32x32.png`, `128x128.png`, `128x128@2x.png`,
`icon.icns`, `icon.ico`) sürüm kontrolüne dahil edilmez ve derleme öncesi tek komutla üretilir:

```bash
# 1024x1024 bir kaynak PNG'den tüm platform ikonlarını üretir
npm run tauri icon path/to/logo.png
```

Kaynak logo eklenene kadar geçici olarak Tauri'nin varsayılan ikonu da kullanılabilir.
`tauri.conf.json > bundle.icon` bu dosyaları referanslar.
