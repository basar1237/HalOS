// Giriş ekranı — gerçek Identity /auth/login (Gateway). 2FA gerekiyorsa kod alanı açılır.

import { useState } from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import { isApiError } from '@/lib/api';
import { useAuth } from '@/lib/auth';

const TWO_FACTOR_REQUIRED = 'User.TwoFactorRequired';

export default function LoginScreen() {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [twoFactorRequired, setTwoFactorRequired] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    setError(null);
    setSubmitting(true);
    try {
      await login({
        email: email.trim(),
        password,
        twoFactorCode: twoFactorCode.trim() || undefined,
      });
      // Yönlendirme kök yerleşimde (oturum durumuna göre).
    } catch (err) {
      if (isApiError(err) && err.code === TWO_FACTOR_REQUIRED) {
        setTwoFactorRequired(true);
        setError('İki adımlı doğrulama kodunuzu girin.');
      } else if (isApiError(err)) {
        setError(err.message);
      } else {
        setError('Giriş yapılamadı. Bilgilerinizi kontrol edin.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <KeyboardAvoidingView
      style={styles.page}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <View style={styles.card}>
        <Text style={styles.brand}>HalOS</Text>
        <Text style={styles.subtitle}>Patron Uygulaması</Text>

        {error ? <Text style={styles.error}>{error}</Text> : null}

        <Text style={styles.label}>E-posta</Text>
        <TextInput
          style={styles.input}
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
          autoComplete="email"
        />

        <Text style={styles.label}>Parola</Text>
        <TextInput
          style={styles.input}
          value={password}
          onChangeText={setPassword}
          secureTextEntry
        />

        {twoFactorRequired ? (
          <>
            <Text style={styles.label}>Doğrulama Kodu</Text>
            <TextInput
              style={styles.input}
              value={twoFactorCode}
              onChangeText={setTwoFactorCode}
              keyboardType="number-pad"
            />
          </>
        ) : null}

        <Pressable
          style={[styles.button, submitting && styles.buttonDisabled]}
          onPress={handleSubmit}
          disabled={submitting}
        >
          {submitting ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.buttonText}>Giriş Yap</Text>
          )}
        </Pressable>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, backgroundColor: '#f4f6f8', justifyContent: 'center', padding: 20 },
  card: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 28,
    borderWidth: 1,
    borderColor: '#e2e8f0',
  },
  brand: { fontSize: 26, fontWeight: '700', color: '#166534', textAlign: 'center' },
  subtitle: { textAlign: 'center', color: '#6b7280', marginBottom: 20 },
  label: { fontSize: 13, color: '#1f2937', marginBottom: 6, marginTop: 12 },
  input: {
    borderWidth: 1,
    borderColor: '#e2e8f0',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 15,
  },
  button: {
    backgroundColor: '#15803d',
    borderRadius: 8,
    paddingVertical: 13,
    alignItems: 'center',
    marginTop: 22,
  },
  buttonDisabled: { opacity: 0.6 },
  buttonText: { color: '#fff', fontWeight: '600', fontSize: 15 },
  error: { color: '#b91c1c', fontSize: 13, marginBottom: 8 },
});
