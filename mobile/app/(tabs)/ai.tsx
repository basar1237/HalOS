// AI Asistan — patron doğal dille sorar (yerel Ollama / Claude). Salt-okuma; veri işletmede kalır.
import { useState } from 'react';
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';

import { api, isApiError } from '@/lib/api';

interface AskResponse {
  answer: string;
  model?: string;
}

const SAMPLES = [
  'Bu ay satışlarım nasıl?',
  'Toplam komisyon gelirim ne kadar?',
  'Bekleyen hakedişim ne kadar?',
];

export default function AiScreen() {
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState<string | null>(null);
  const [model, setModel] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function ask(q?: string) {
    const text = (q ?? question).trim();
    if (!text || loading) return;
    setQuestion(text);
    setLoading(true);
    setError(null);
    setAnswer(null);
    try {
      const res = await api.post<AskResponse>('/ai/ask', { question: text });
      setAnswer(res.answer);
      setModel(res.model ?? null);
    } catch (e) {
      setError(isApiError(e) ? e.message : 'AI yanıt vermedi.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView style={styles.screen} contentContainerStyle={styles.content}>
      <Text style={styles.lead}>Doğal dille sorun — yapay zeka muhasebeciniz cevaplasın.</Text>

      <TextInput
        style={styles.input}
        value={question}
        onChangeText={setQuestion}
        placeholder="Örn: Bu ay kârım ne kadar?"
        placeholderTextColor="#9ca3af"
        multiline
        onSubmitEditing={() => ask()}
        returnKeyType="send"
      />

      <TouchableOpacity style={[styles.btn, loading && styles.btnDisabled]} onPress={() => ask()} disabled={loading}>
        <Text style={styles.btnText}>{loading ? 'Düşünüyor…' : 'Sor'}</Text>
      </TouchableOpacity>

      <View style={styles.chips}>
        {SAMPLES.map((s) => (
          <TouchableOpacity key={s} style={styles.chip} onPress={() => ask(s)}>
            <Text style={styles.chipText}>{s}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <View style={styles.answerBox}>
          <ActivityIndicator color="#15803d" />
          <Text style={styles.muted}>Yerel model yanıt hazırlıyor, biraz sürebilir…</Text>
        </View>
      ) : null}

      {error ? <Text style={styles.error}>{error}</Text> : null}

      {answer ? (
        <View style={styles.answerBox}>
          <Text style={styles.answer}>{answer}</Text>
          {model ? <Text style={styles.modelTag}>model: {model}</Text> : null}
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#f4f6f8' },
  content: { padding: 16 },
  lead: { color: '#6b7280', fontSize: 14, marginBottom: 14 },
  input: {
    backgroundColor: '#fff', borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10,
    padding: 12, fontSize: 16, minHeight: 60, textAlignVertical: 'top', color: '#1f2937',
  },
  btn: { backgroundColor: '#15803d', borderRadius: 10, padding: 14, alignItems: 'center', marginTop: 12 },
  btnDisabled: { opacity: 0.6 },
  btnText: { color: '#fff', fontWeight: '700', fontSize: 16 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginTop: 14 },
  chip: { backgroundColor: '#ecfdf3', borderWidth: 1, borderColor: '#bbf7d0', borderRadius: 999, paddingVertical: 6, paddingHorizontal: 12 },
  chipText: { color: '#166534', fontSize: 13 },
  answerBox: { backgroundColor: '#fff', borderWidth: 1, borderColor: '#e2e8f0', borderRadius: 10, padding: 16, marginTop: 16 },
  answer: { color: '#1f2937', fontSize: 15, lineHeight: 22 },
  modelTag: { color: '#9ca3af', fontSize: 12, marginTop: 10 },
  muted: { color: '#6b7280', fontSize: 13, marginTop: 8 },
  error: { color: '#b91c1c', fontSize: 14, marginTop: 12 },
});
