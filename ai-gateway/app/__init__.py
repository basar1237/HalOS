"""HalOS AI Gateway — ERP ile LLM arasındaki güvenli sınır (docs/04 ADR-002).

Bu paket, ERP servislerinden YALNIZCA OKUYAN (yazma yok) ve gelen kullanıcı
sorularını Claude'a (veya anahtarsız ortamda deterministik Stub'a) yönlendiren
FastAPI servisini içerir. Güvenlik sınırı: gelen JWT doğrulanır, tenant_id + role
claim'leri çıkarılır ve ERP sorguları tenant'a kapsanır (ADR-002, docs/04 §7).
"""

__all__ = []
