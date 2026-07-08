'use client';

// Giriş (login) sayfası — gerçek Identity /auth/login akışına bağlı (auth-context, ADR-009).
// 2FA etkin kullanıcıda Identity "User.TwoFactorRequired" (401) döner → doğrulama kodu alanı
// açılır ve kullanıcı kodu girip yeniden dener.

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { useAuth } from '@/features/auth/auth-context';
import { isApiError } from '@/lib/api-client';

const TWO_FACTOR_REQUIRED = 'User.TwoFactorRequired';

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [twoFactorCode, setTwoFactorCode] = useState('');
  const [twoFactorRequired, setTwoFactorRequired] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({
        email,
        password,
        twoFactorCode: twoFactorCode.trim() || undefined,
      });
      router.replace('/dashboard');
    } catch (err) {
      // 2FA gerekiyorsa kod alanını aç; diğer hatalarda Identity'nin mesajını göster.
      if (isApiError(err) && err.code === TWO_FACTOR_REQUIRED) {
        setTwoFactorRequired(true);
        setError('İki adımlı doğrulama kodunuzu girin.');
      } else if (isApiError(err)) {
        setError(err.message);
      } else {
        setError('Giriş yapılamadı. Bilgilerinizi kontrol edin.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-card__brand">HalOS</div>
        <p className="login-card__subtitle">Yönetim Konsolu</p>

        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-field">
          <label htmlFor="email">E-posta</label>
          <input
            id="email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="form-field">
          <label htmlFor="password">Parola</label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        {twoFactorRequired ? (
          <div className="form-field">
            <label htmlFor="twoFactorCode">Doğrulama Kodu</label>
            <input
              id="twoFactorCode"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              value={twoFactorCode}
              onChange={(e) => setTwoFactorCode(e.target.value)}
              required
              autoFocus
            />
          </div>
        ) : null}

        <button type="submit" className="btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş Yap'}
        </button>
      </form>
    </div>
  );
}
