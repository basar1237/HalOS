import { ComingSoon } from '@/components/coming-soon';

// Satış & Komisyon — satış işlemi liste/detay ekranı. Backend'de rapor uçları (günlük özet,
// komisyon geliri, trend) var; işlem-bazlı liste ucu eklenince bu sayfa bağlanacak.
export default function SalesPage() {
  return (
    <ComingSoon
      title="Satış & Komisyon"
      note="Satış işlemi listesi ve komisyon/hakediş detayı bu ekrana gelecek. Günlük özet şu an Kontrol Paneli'nde canlı görünüyor."
    />
  );
}
