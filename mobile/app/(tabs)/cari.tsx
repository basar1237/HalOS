// Cari — cari hesap bakiyeleri listesi (Gateway /api/finance/current-accounts).

import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';

import { formatTRY } from '@/lib/format';
import { useQuery } from '@/lib/use-query';
import type { CurrentAccount, PagedResult } from '@/shared/types';

export default function CurrentAccountsScreen() {
  const { data, loading, error } = useQuery<PagedResult<CurrentAccount>>(
    '/api/finance/current-accounts?page=1&pageSize=30',
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
      keyExtractor={(a) => a.id}
      contentContainerStyle={styles.content}
      ListEmptyComponent={<Text style={styles.empty}>Cari hesap yok.</Text>}
      renderItem={({ item }) => (
        <View style={styles.row}>
          <View style={styles.rowMain}>
            <Text style={styles.rowTitle}>Taraf #{item.partyId.slice(0, 8)}</Text>
            <Text style={styles.rowSub}>{item.entryCount} hareket</Text>
          </View>
          <Text
            style={[
              styles.balance,
              { color: item.balance < 0 ? '#b91c1c' : '#166534' },
            ]}
          >
            {formatTRY(item.balance)}
          </Text>
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
  balance: { fontSize: 15, fontWeight: '700' },
  empty: { textAlign: 'center', color: '#6b7280', marginTop: 40 },
  error: { color: '#b91c1c' },
});
