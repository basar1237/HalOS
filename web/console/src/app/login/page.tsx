'use client';

// Giriş (login) sayfası — form iskeleti. Gerçek Identity servisi çağrısı auth-context
// içindeki login stub'una bağlı (ADR-009). Metinler Türkçe.

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { useAuth } from '@/features/auth/auth-context';

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login({ email, password });
      router.replace('/dashboard');
    } catch {
      setError('Giriş yapılamadı. Bilgilerinizi kontrol edin.');
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

        <button type="submit" className="btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş Yap'}
        </button>
      </form>
    </div>
  );
}
