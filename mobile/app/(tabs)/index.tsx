// Panel — patron için canlı özet kartları (Gateway rapor uçları). Web dashboard'ının mobil karşılığı.

import { ScrollView, StyleSheet, Text, View } from 'react-native';

import { formatTRY, todayIso } from '@/lib/format';
import { useAuth } from '@/lib/auth';
import { useQuery } from '@/lib/use-query';
import type {
  AgingReport,
  DailySalesSummary,
  PendingDocuments,
  SalesDashboard,
} from '@/shared/types';

function Card({
  title,
  value,
  sub,
  loading,
  error,
}: {
  title: string;
  value?: string;
  sub?: string;
  loading: boolean;
  error: string | null;
}) {
  return (
    <View style={styles.card}>
      <Text style={styles.cardTitle}>{title}</Text>
      {loading ? (
        <Text style={styles.cardMuted}>Yükleniyor…</Text>
      ) : error ? (
        <Text style={styles.cardError}>{error}</Text>
      ) : (
        <>
          <Text style={styles.cardValue}>{value ?? '—'}</Text>
          {sub ? <Text style={styles.cardMuted}>{sub}</Text> : null}
        </>
      )}
    </View>
  );
}

export default function PanelScreen() {
  const { user, logout } = useAuth();
  const daily = useQuery<DailySalesSummary>(`/api/sales/reports/daily?day=${todayIso()}`);
  const dash = useQuery<SalesDashboard>(`/api/sales/reports/dashboard?day=${todayIso()}`);
  const aging = useQuery<AgingReport>('/api/finance/reports/aging');
  const docs = useQuery<PendingDocuments>('/api/integration/reports/pending-documents');

  return (
    <ScrollView style={styles.page} contentContainerStyle={styles.content}>
      <Text style={styles.greeting}>Merhaba, {user?.fullName ?? 'Patron'}</Text>

      <Card
        title="Günlük Satış (net)"
        value={daily.data ? formatTRY(daily.data.net) : undefined}
        sub={daily.data ? `${daily.data.count} satış · bugün` : undefined}
        loading={daily.loading}
        error={daily.error}
      />
      <Card
        title="Bekleyen Hakediş"
        value={dash.data ? formatTRY(dash.data.pendingSettlementTotal) : undefined}
        loading={dash.loading}
        error={dash.error}
      />
      <Card
        title="Açık Cari Bakiye"
        value={aging.data ? formatTRY(aging.data.totalAmount) : undefined}
        sub={aging.data ? `${aging.data.totalAccountCount} cari` : undefined}
        loading={aging.loading}
        error={aging.error}
      />
      <Card
        title="Bugünkü Mal Geliş"
        value={dash.data ? String(dash.data.todayConsignmentCount) : undefined}
        sub="parti · bugün"
        loading={dash.loading}
        error={dash.error}
      />
      <Card
        title="Bekleyen e-Belge"
        value={docs.data ? String(docs.data.total) : undefined}
        sub={
          docs.data
            ? `${docs.data.pendingInvoices} e-Fatura · ${docs.data.pendingProducerReceipts} e-MM · ${docs.data.pendingHksNotifications} HKS`
            : undefined
        }
        loading={docs.loading}
        error={docs.error}
      />

      <Text style={styles.logout} onPress={() => void logout()}>
        Çıkış Yap
      </Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, backgroundColor: '#f4f6f8' },
  content: { padding: 16, gap: 12 },
  greeting: { fontSize: 16, fontWeight: '600', color: '#1f2937', marginBottom: 4 },
  card: {
    backgroundColor: '#fff',
    borderRadius: 10,
    padding: 16,
    borderWidth: 1,
    borderColor: '#e2e8f0',
  },
  cardTitle: { fontSize: 13, color: '#6b7280', marginBottom: 6 },
  cardValue: { fontSize: 24, fontWeight: '700', color: '#1f2937' },
  cardMuted: { fontSize: 12, color: '#6b7280', marginTop: 4 },
  cardError: { fontSize: 12, color: '#b91c1c' },
  logout: {
    textAlign: 'center',
    color: '#b91c1c',
    fontWeight: '600',
    paddingVertical: 16,
  },
});
