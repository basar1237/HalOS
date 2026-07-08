# HalOS AI Gateway — container imajı (docs/04 §9 docker-compose ile hizalı).
FROM python:3.12-slim

# Deterministik, tampon'suz log çıktısı
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

WORKDIR /app

# Önce bağımlılıklar (katman önbelleği)
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Uygulama kodu
COPY app ./app

EXPOSE 8080

# ANTHROPIC_API_KEY vb. ortam değişkeninden gelir (sır koda yazılmaz).
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8080"]
