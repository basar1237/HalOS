'use client';

// Auth akışı iskeleti — ADR-009 (JWT + refresh + 2FA, merkezi Identity servisi).
// Bu faz: token saklama yeri + oturum durumu + login/logout stub'ları.
// Gerçek Identity servisi çağrıları ileriki fazda bağlanacak (docs/06).

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { clearTokens, getAccessToken, saveTokens } from '@/lib/token-storage';
import type { AuthTokens, LoginCredentials, User } from '@/shared/types';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  /** Sayfa yüklenirken token kontrolü tamamlanana kadar true. */
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // İlk yüklemede mevcut token'ı kontrol et (oturum kurtarma iskeleti).
  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      // TODO(faz): Identity servisinden /me çağrısı ile kullanıcı çözülecek.
      // Şimdilik yalnızca token varlığından oturumu türetiyoruz (stub).
      setUser({
        id: 'stub',
        fullName: 'Oturum Kullanıcısı',
        email: '',
        roles: [],
      });
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (credentials: LoginCredentials) => {
    // TODO(faz): apiClient.post('/auth/login', credentials) ile Identity servisine bağlan.
    // İskelet: gelen token'ları sakla, kullanıcıyı ata.
    void credentials;
    const tokens: AuthTokens = {
      accessToken: 'stub-access-token',
      refreshToken: 'stub-refresh-token',
    };
    saveTokens(tokens);
    setUser({
      id: 'stub',
      fullName: 'Oturum Kullanıcısı',
      email: credentials.email,
      roles: [],
    });
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      login,
      logout,
    }),
    [user, isLoading, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth, AuthProvider içinde kullanılmalıdır.');
  }
  return context;
}
