// Satışlar — son satışlar listesi (Gateway /api/sales/sales, sold_at azalan).

import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';

import { formatDate, formatTRY } from '@/lib/format';
import { useQuery } from '@/lib/use-query';
import type { PagedResult, Sale } from '@/shared/types';

const STATUS: Record<number, string> = { 1: 'Taslak', 2: 'Tamamlandı', 3: 'İptal' };

export default function SalesScreen() {
  const { data, loading, error } = useQuery<PagedResult<Sale>>(
    '/api/sales/sales?page=1&pageSize=30',
  );

  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color="#15803d" />
      </View>
    );
  }
  if (error) {
    return (
      <View style={styles.center}>
        <Text style={styles.error}>{error}</Text>
      </View>
    );
  }

  return (
    <FlatList
      style={styles.page}
      data={data?.items ?? []}
      keyExtractor={(s) => s.id}
      contentContainerStyle={styles.content}
      ListEmptyComponent={<Text style={styles.empty}>Satış kaydı yok.</Text>}
      renderItem={({ item }) => (
        <View style={styles.row}>
          <View style={styles.rowMain}>
            <Text style={styles.rowTitle}>{formatTRY(item.grossAmount)}</Text>
            <Text style={styles.rowSub}>{formatDate(item.soldAt)}</Text>
          </View>
          <Text style={styles.badge}>{STATUS[item.status] ?? '—'}</Text>
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, backgroundColor: '#f4f6f8' },
  content: { padding: 16, gap: 8 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: '#f4f6f8' },
  row: {
    backgroundColor: '#fff',
    borderRadius: 10,
    padding: 14,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  rowMain: { gap: 2 },
  rowTitle: { fontSize: 15, fontWeight: '600', color: '#1f2937' },
  rowSub: { fontSize: 12, color: '#6b7280' },
  badge: { fontSize: 12, fontWeight: '600', color: '#166534' },
  empty: { textAlign: 'center', color: '#6b7280', marginTop: 40 },
  error: { color: '#b91c1c' },
});
