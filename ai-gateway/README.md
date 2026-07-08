# HalOS AI Gateway (Python / FastAPI)

ERP ile LLM arasındaki **güvenli sınır** (docs/04 **ADR-002**, docs/06 **S2.4**). İlk AI
ajanı: **AI muhasebeci** (docs/01, docs/03). .NET servislerinden **ayrı** bir Python
servisidir; `HalOS.sln`'e dokunmaz.

## Güvenlik sınırı (ADR-002)

- Gateway ERP'den **YALNIZCA OKUR** — hiçbir yazma metodu yoktur (`erp_client.py`).
- Gelen **JWT** doğrulanır (HS256), ERP (Identity) ile **aynı** imza anahtarı/issuer/
  audience kullanılır. Token'dan `tenant_id` ve `role` claim'leri çıkarılır (docs/04 §7).
- Sorgular kullanıcının token'ı ile ERP'ye iletilir; tenant kapsaması korunur (BK-8).
- Rapor okuma yetkisi: **Owner / Manager / Accountant** (aksi halde 403).
- **İmza anahtarı fail-fast:** `JWT__SIGNING_KEY` non-development ortamda (`APP_ENV` /
  `ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` development değilse; hiçbiri yoksa güvenli
  varsayılan **Production**) eksik, 32 bayttan kısa VEYA repoya işlenmiş geliştirme
  varsayılanına eşitse servis başlangıçta **RuntimeError** ile durur. Bu, .NET
  servislerindeki `JwtSigningKeyResolver` ile **simetriktir** (docs/07 §güvenlik):
  üretimde tahmin edilebilir anahtarla sessizce token doğrulanmasını engeller.

## Anahtarsız çalışma (Stub)

`ANTHROPIC_API_KEY` **boşsa** servis yine çalışır: gerçek Claude çağrısı yapılmaz,
deterministik Türkçe cevap üreten **`StubLlmClient`** devreye girer. Anahtar
tanımlandığında otomatik olarak gerçek **Claude** (Messages API, `ANTHROPIC_MODEL`,
varsayılan `claude-sonnet-4-6`) kullanılır. Anahtar istenmez / uydurulmaz.

## Uçlar

| Metot | Yol        | Açıklama                                                        |
|-------|------------|-----------------------------------------------------------------|
| GET   | `/health`  | Sağlık + aktif LLM (`stub`/model adı).                          |
| POST  | `/ai/ask`  | `require_accountant` → ERP raporlarını oku → Claude/Stub → yanıt. |

### `POST /ai/ask`

İstek gövdesi:

```json
{ "question": "Bu ayki satış ve cari durumumuz nedir?", "from": "2026-06-01", "to": "2026-06-30", "asOf": "2026-06-30" }
```

`from`/`to`/`asOf` opsiyoneldir (varsayılan: son 30 gün / bugün). Yanıt:

```json
{ "answer": "...", "used_sources": ["sales:/reports/sales-summary", "finance:/reports/aging"], "model": "stub" }
```

Çekilen ERP raporları (SALT-OKUMA):
- Sales: `GET /reports/sales-summary?from&to`
- Finance: `GET /reports/aging?asOf`

## Yerel çalıştırma

```bash
cd ai-gateway
python -m venv .venv
# Windows: .venv\Scripts\activate    | Unix: source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env          # ANTHROPIC_API_KEY boş bırakılabilir
uvicorn app.main:app --reload --port 8000
```

## Test

Anahtarsız (stub) yeşil olmalı; gerçek anahtar/ERP **gerekmez** (DI ile stub enjekte edilir):

```bash
pytest
```

## Yapı

```
ai-gateway/
├── app/
│   ├── config.py       # pydantic-settings (Anthropic, JWT, ERP URL'leri)
│   ├── llm.py          # LlmClient protokolü + Anthropic/Stub + build_llm_client
│   ├── erp_client.py   # ErpReadClient protokolü + Http/Stub (SALT-OKUMA)
│   ├── auth.py         # JWT (HS256) doğrulama + require_accountant (401/403)
│   ├── prompts.py      # build_accountant_prompt (Türkçe sistem promptu)
│   └── main.py         # FastAPI: /health, /ai/ask
├── tests/              # pytest + FastAPI TestClient (stub'lı)
├── requirements.txt
├── Dockerfile
└── .env.example
```
