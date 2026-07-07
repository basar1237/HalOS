# CI ve Branch Koruma

> **Durum:** Taslak v0.1 · **Dil:** Türkçe
> **Dayanak:** 07 §9 (Commit/Branch/PR), 04 §9 (Deployment / CI-CD).
> Bu doküman CI hattını ve `main`/`develop` dallarının koruma kurallarını özetler.

---

## 1. CI Hattı (`.github/workflows/ci.yml`)

Her **pull request** ve `main`/`develop`'a **push**'ta çalışır.

| Job | Ne yapar | Dayanak |
|-----|----------|---------|
| **backend** (.NET Build & Test) | `actions/setup-dotnet@v4` (8.0.x) → `dotnet restore` / `build` / `test HalOS.sln` | ADR-001, 07 §7 |
| **web-console** (Node 20 / 22 matris) | `actions/setup-node@v4` → bağımlılık kur → `type-check` + `lint` + `build` | ADR-003, 07 §7 |

- Aynı ref için eski çalışmalar iptal edilir (`concurrency` + `cancel-in-progress`).
- İzin en aza indirilmiştir (`permissions: contents: read`).

---

## 2. Branch Koruma Kuralları (`main` ve `develop`)

07 §9 gereği **main korumalıdır; doğrudan push yasak.** GitHub → Settings → Branches → Branch protection rules:

- [ ] **Require a pull request before merging** (doğrudan push kapalı).
  - [ ] En az **1 onay** (07 §9: "en az bir onay").
  - [ ] Yeni push gelince eski onaylar geçersiz (stale review dismissal).
- [ ] **Require status checks to pass before merging** (07 §7: test yeşil olmadan merge yok).
  - [ ] Require branches to be up to date before merging.
  - Zorunlu check'ler:
    - `.NET Build & Test`
    - `Web Console (Node 20)`
    - `Web Console (Node 22)`
- [ ] **Require conversation resolution before merging.**
- [ ] **Require linear history** (temiz geçmiş; squash/rebase merge).
- [ ] **Do not allow bypassing the above settings** (yöneticiler dahil).
- [ ] **Restrict force pushes** ve **restrict deletions** (`main`/`develop` silinemez/force-push edilemez).

> `--no-verify` / hook atlama / imza bypass **yasak** (07 §9), kullanıcı açıkça istemedikçe.

---

## 3. Branch ve PR Konvansiyonu (07 §9 özet)

- **Branch:** `feature/<kısa-ad>`, `fix/<kısa-ad>`, `chore/<kısa-ad>`.
- **Commit:** Conventional Commits — `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- **PR:** küçük ve odaklı; açıklamada **ne + neden + hangi BK/ADR**; test kanıtı; ilgili doküman güncellendi mi.
- **Merge:** yalnızca tüm zorunlu check'ler yeşil + gerekli onay(lar) alındıktan sonra.

---

## 4. Notlar / Yapılacaklar

- `web/console` içinde henüz **lockfile yok.** CI şu an lockfile varsa `npm ci`, yoksa `npm install`
  kullanır. `package-lock.json` commit'lenince tekrarlanabilir kurulum için `npm ci` devreye girer.
- Henüz **.NET test projesi yok**; `dotnet test` proje bulamazsa hatasız (exit 0) geçer.
  Test projeleri eklendikçe otomatik kapsanır (07 §7 katman tablosu).
- Bütünleşme testleri için Testcontainers/Postgres (07 §7) ileride ayrı bir CI job'ı gerektirebilir.
