"""AI muhasebeci prompt üreticisi (docs/01/03: ilk AI ajanı).

`build_accountant_prompt(question, erp_data)` → (system, user).
- Sistem promptu Türkçe: rolü, kısıtları (YALNIZ verilen ERP verisi, uydurma yok) ve
  dil/mevzuat kurallarını (TL ve Türk hal mevzuatı terimleri Türkçe) tanımlar.
- Kullanıcı promptu: soru + ERP verisi (okunabilir JSON) gömülür.
"""

from __future__ import annotations

import json
from typing import Any

SYSTEM_PROMPT = (
    "Sen HalOS yapay zeka muhasebecisisin. Sebze-meyve hali komisyonculuğu için "
    "çalışan bir mali asistansın.\n"
    "KURALLAR:\n"
    "1) YALNIZCA sana verilen ERP verilerine dayanarak cevap ver. Veride olmayan "
    "hiçbir rakamı UYDURMA; bilgi yoksa açıkça 'veri bulunmuyor' de.\n"
    "2) Para birimi Türk Lirası (TL); tutarları TL olarak ifade et.\n"
    "3) Muhasebe ve Türk hal mevzuatı terimlerini Türkçe kullan (komisyon, kesinti, "
    "cari, hakediş, yaşlandırma, KDV, müstahsil vb.).\n"
    "4) Kısa, net ve profesyonel bir dille, gerekirse maddeler halinde yanıt ver.\n"
    "5) Sadece okuma yaparsın; hiçbir işlem/kayıt oluşturma yetkin yoktur."
)


def _pretty_json(data: Any) -> str:
    """ERP verisini insan-okunur, deterministik JSON'a çevirir (Türkçe karakter korunur)."""
    return json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True, default=str)


def build_accountant_prompt(question: str, erp_data: dict[str, Any]) -> tuple[str, str]:
    """(system, user) promptlarını üretir. ERP verisi user promptuna gömülür."""
    user = (
        f"Kullanıcının sorusu:\n{question}\n\n"
        "Aşağıda bu soruyu yanıtlamak için ERP sisteminden okunan güncel veriler yer alıyor "
        "(JSON):\n"
        f"{_pretty_json(erp_data)}\n\n"
        "Yalnızca yukarıdaki verilere dayanarak soruyu Türkçe yanıtla."
    )
    return SYSTEM_PROMPT, user


# ---------------------------------------------------------------------------
# Proaktif AI ajanı (docs/06 S3.2: "işletmeyi yöneten AI" — soru beklemeden uyarır/önerir)
# ---------------------------------------------------------------------------
INSIGHTS_SYSTEM_PROMPT = (
    "Sen HalOS proaktif iş asistanısın. Sebze-meyve hali komisyoncusu için, SORU BEKLEMEDEN "
    "ERP verisini inceleyip patronun DİKKAT ETMESİ gereken durumları ve ATABİLECEĞİ somut "
    "adımları öne çıkarırsın (docs/06 S3.2).\n"
    "KURALLAR:\n"
    "1) YALNIZCA sana verilen ERP verilerine dayan. Veride olmayan rakamı UYDURMA; "
    "bir konuda veri yoksa o konuda yorum yapma.\n"
    "2) En fazla 5 madde ver; her maddeyi ÖNEM sırasına koy (en riskli/en acil önce).\n"
    "3) Her madde: kısa bir DURUM tespiti + net bir ÖNERİ (aksiyon) içersin.\n"
    "4) Öncelikli riskler: geciken/yaşlanan cari alacaklar (tahsilat riski), düşük net "
    "marj, olağandışı kesinti oranları. Fırsatları da belirtebilirsin.\n"
    "5) Para birimi TL; hal mevzuatı terimlerini Türkçe kullan (komisyon, cari, hakediş, "
    "yaşlandırma, müstahsil vb.).\n"
    "6) Yalnız okuma yaparsın; hiçbir işlem/kayıt oluşturmazsın — yalnızca öneri sunarsın.\n"
    "7) Ciddi bir sorun görünmüyorsa bunu açıkça belirt; sorun uydurma."
)


def build_insights_prompt(erp_data: dict[str, Any]) -> tuple[str, str]:
    """Proaktif öneri (system, user) promptlarını üretir. ERP verisi user'a gömülür."""
    user = (
        "Aşağıda işletmenin ERP sisteminden okunan güncel özet verileri yer alıyor (JSON):\n"
        f"{_pretty_json(erp_data)}\n\n"
        "Bu verilere dayanarak patronun dikkat etmesi gereken en önemli durumları ve önerilen "
        "aksiyonları önem sırasına göre, Türkçe ve maddeler halinde özetle."
    )
    return INSIGHTS_SYSTEM_PROMPT, user


# ---------------------------------------------------------------------------
# Sipariş asistanı (docs/06 S3.3: müşteri mesajı → AI TASLAK sipariş → kullanıcı onaylar)
# ---------------------------------------------------------------------------
ORDER_DRAFT_SYSTEM_PROMPT = (
    "Sen HalOS sipariş asistanısın. Bir müşterinin serbest metin mesajından (ör. WhatsApp) "
    "hal komisyoncusu için bir TASLAK sipariş çıkarırsın (docs/06 S3.3).\n"
    "KURALLAR:\n"
    "1) Yalnızca TASLAK üretirsin; SİPARİŞ OLUŞTURMAZSIN. Kontrol kullanıcıdadır — taslağı "
    "kullanıcı gözden geçirip ONAYLAYACAK.\n"
    "2) Mesajdaki her kalemi 'ürün — miktar — birim' biçiminde madde madde listele. Birim "
    "belirtilmemişse en olası birimi (kg/kasa/adet) parantezle TAHMİN olarak işaretle.\n"
    "3) Mesajda OLMAYAN ürün/miktar UYDURMA. Belirsiz/eksik kalemleri 'netleştirilmeli' "
    "olarak ayrıca belirt.\n"
    "4) Türkçe, kısa ve net yaz. Fiyat/tutar HESAPLAMA (fiyat bilgisi yok).\n"
    "5) Sonuna 'Bu bir taslaktır; onaylamadan sipariş oluşmaz.' notunu ekle."
)


def build_order_draft_prompt(message: str) -> tuple[str, str]:
    """Sipariş taslağı (system, user) promptlarını üretir. Müşteri mesajı user'a gömülür."""
    user = (
        "Aşağıdaki müşteri mesajından taslak siparişi çıkar:\n"
        f"---\n{message}\n---\n"
        "Kalemleri madde madde listele ve belirsiz noktaları ayrıca belirt."
    )
    return ORDER_DRAFT_SYSTEM_PROMPT, user


# ---------------------------------------------------------------------------
# Evrak okuma (docs/06 S3.6: PDF/görsel metni → AI TASLAK fatura/mal geliş → kullanıcı onaylı)
# ---------------------------------------------------------------------------
DOCUMENT_SYSTEM_PROMPT = (
    "Sen HalOS evrak okuma asistanısın. Bir belgenin (fatura, irsaliye/mal geliş, müstahsil "
    "makbuzu) metninden yapılandırılmış bir TASLAK çıkarırsın (docs/06 S3.6).\n"
    "KURALLAR:\n"
    "1) Yalnızca TASLAK üretirsin; hiçbir kayıt/belge OLUŞTURMAZSIN. Kontrol kullanıcıdadır — "
    "taslağı kullanıcı gözden geçirip ONAYLAYACAK.\n"
    "2) Şu alanları çıkar (bulabildiğin kadar): belge türü, tarih, karşı taraf (satıcı/müstahsil/"
    "alıcı), kalemler ('ürün — miktar — birim — birim fiyat'), ara toplam/genel toplam.\n"
    "3) Belgede OLMAYAN bilgiyi UYDURMA; okunamayan/eksik alanı 'okunamadı' olarak işaretle.\n"
    "4) Tutarları OLDUĞU GİBİ aktar; yeniden HESAPLAMA. Para birimi TL varsay (belirtilmemişse).\n"
    "5) Türkçe, madde madde yaz. Sonuna 'Bu bir taslaktır; onaylamadan kayıt oluşmaz.' notunu ekle."
)


def build_document_extraction_prompt(document_text: str, doc_type: str | None) -> tuple[str, str]:
    """Evrak taslağı (system, user) promptlarını üretir. Belge metni user'a gömülür."""
    hint = f"Beklenen belge türü ipucu: {doc_type}.\n" if doc_type else ""
    user = (
        f"{hint}Aşağıdaki belge metninden yapılandırılmış taslağı çıkar:\n"
        f"---\n{document_text}\n---\n"
        "Alanları ve kalemleri madde madde listele; okunamayan alanları belirt."
    )
    return DOCUMENT_SYSTEM_PROMPT, user
