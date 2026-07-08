import { ComingSoon } from '@/components/coming-soon';

// Soğuk Zincir & IoT — soğuk oda/sensör/sıcaklık izleme (docs/02). ColdChain/IoT servisi henüz
// kurulmadı (MQTT/EMQX omurgası sonraki faz); hazır olunca canlı sıcaklık/alarm ekranı gelecek.
export default function ColdChainPage() {
  return (
    <ComingSoon
      title="Soğuk Zincir & IoT"
      note="Soğuk oda ve sensör sıcaklık izleme, alarm eşikleri bu ekrana gelecek. ColdChain/IoT servisi (MQTT) henüz kurulmadı."
    />
  );
}
