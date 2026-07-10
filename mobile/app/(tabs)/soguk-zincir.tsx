// Soğuk Zincir — patron için soğuk oda sıcaklık/alarm görünümü (Gateway /api/coldchain,
// docs/04 §6, S3.1). Son sıcaklık izin verilen aralığın dışındaysa ALARM (kırmızı) gösterilir.

import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';

import { formatDate } from '@/lib/format';
import { useQuery } from '@/lib/use-query';
import type { ColdStorageUnit, PagedResult } from '@/shared/types';

function isBreaching(u: ColdStorageUnit): boolean {
  if (u.latestTemperatureC == null) return false;
  return u.latestTemperatureC > u.maxTempC || u.latestTemperatureC < u.minTempC;
}

function tempText(value: number | null): string {
  return value == null ? '—' : `${value.toFixed(1)} °C`;
}

export default function ColdChainScreen() {
  const { data, loading, error } = useQuery<PagedResult<ColdStorageUnit>>(
    '/api/coldchain/cold-storage-units?page=1&pageSize=50',
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
      keyExtractor={(u) => u.id}
      contentContainerStyle={styles.content}
      ListEmptyComponent={<Text style={styles.empty}>Tanımlı soğuk oda yok.</Text>}
      renderItem={({ item }) => {
        const breach = isBreaching(item);
        const noData = item.latestTemperatureC == null;
        return (
          <View style={[styles.row, breach && styles.rowAlarm]}>
            <View style={styles.rowMain}>
              <Text style={styles.rowTitle}>{item.name}</Text>
              <Text style={styles.rowSub}>
                Aralık: {item.minTempC.toFixed(1)} … {item.maxTempC.toFixed(1)} °C
              </Text>
              {item.latestReadingAt ? (
                <Text style={styles.rowSub}>Son okuma: {formatDate(item.latestReadingAt)}</Text>
              ) : null}
            </View>
            <View style={styles.rowRight}>
              <Text style={[styles.temp, breach && styles.tempAlarm]}>
                {tempText(item.latestTemperatureC)}
              </Text>
              <Text
                style={[
                  styles.badge,
                  breach ? styles.badgeAlarm : noData ? styles.badgeMuted : styles.badgeOk,
                ]}
              >
                {!item.isActive ? 'Pasif' : noData ? 'Veri yok' : breach ? 'ALARM' : 'Normal'}
              </Text>
            </View>
          </View>
        );
      }}
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
  rowAlarm: { borderColor: '#fca5a5', backgroundColor: '#fef2f2' },
  rowMain: { gap: 2, flex: 1 },
  rowRight: { alignItems: 'flex-end', gap: 4 },
  rowTitle: { fontSize: 15, fontWeight: '600', color: '#1f2937' },
  rowSub: { fontSize: 12, color: '#6b7280' },
  temp: { fontSize: 16, fontWeight: '700', color: '#1f2937' },
  tempAlarm: { color: '#b91c1c' },
  badge: { fontSize: 11, fontWeight: '700', overflow: 'hidden', borderRadius: 6, paddingHorizontal: 8, paddingVertical: 2 },
  badgeOk: { color: '#166534', backgroundColor: '#dcfce7' },
  badgeAlarm: { color: '#fff', backgroundColor: '#dc2626' },
  badgeMuted: { color: '#6b7280', backgroundColor: '#e5e7eb' },
  empty: { textAlign: 'center', color: '#6b7280', marginTop: 40 },
  error: { color: '#b91c1c' },
});
