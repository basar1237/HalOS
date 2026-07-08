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
