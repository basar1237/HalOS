'use client';

// Auth akışı — ADR-009 (JWT + refresh + 2FA, merkezi Identity servisi).
// Gerçek Identity uçlarına bağlı: POST /auth/login (token çifti + kullanıcı özeti) ve
// GET /me (oturum kurtarma → tam kullanıcı). Token'lar token-storage'da tutulur.

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { apiClient } from '@/lib/api-client';
import { clearTokens, getAccessToken, saveTokens } from '@/lib/token-storage';
import type {
  AuthenticationResult,
  CurrentUserDto,
  LoginCredentials,
  User,
} from '@/shared/types';

interface AuthContextValue {
  user: User | null;
  /** Aktif tenant (JWT claim kaynağı; /me ve login yanıtından çözülür, BK-8). */
  tenantId: string | null;
  isAuthenticated: boolean;
  /** Sayfa yüklenirken token kontrolü tamamlanana kadar true. */
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/** CurrentUserDto → UI User modeli (rol tekli string → roller dizisi). */
function toUser(dto: CurrentUserDto): User {
  return {
    id: dto.id,
    fullName: dto.fullName,
    email: dto.email,
    roles: dto.role ? [dto.role] : [],
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [tenantId, setTenantId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // İlk yüklemede token varsa /me ile kullanıcıyı çöz (oturum kurtarma). Token geçersiz/
  // süresi dolmuşsa (401) sessizce temizle → login akışına düş.
  useEffect(() => {
    let cancelled = false;

    async function restore() {
      const token = getAccessToken();
      if (!token) {
        setIsLoading(false);
        return;
      }
      try {
        const me = await apiClient.get<CurrentUserDto>('/api/identity/me');
        if (!cancelled) {
          setUser(toUser(me));
          setTenantId(me.tenantId);
        }
      } catch {
        if (!cancelled) {
          clearTokens();
          setUser(null);
          setTenantId(null);
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }

    void restore();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (credentials: LoginCredentials) => {
    // Identity kimlik doğrular, token çifti + kullanıcı özeti döner (AuthenticationResult).
    const result = await apiClient.post<AuthenticationResult>(
      '/api/identity/auth/login',
      credentials,
    );

    saveTokens({
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
    });
    setTenantId(result.tenantId);

    // Görünen ad login yanıtında yok → /me ile tam kullanıcıyı çöz. /me başarısız olsa bile
    // giriş geçerli; login yanıtındaki özetle asgari kullanıcıyı kur.
    try {
      const me = await apiClient.get<CurrentUserDto>('/api/identity/me');
      setUser(toUser(me));
      setTenantId(me.tenantId);
    } catch {
      setUser({
        id: result.userId,
        fullName: result.email,
        email: result.email,
        roles: result.role ? [result.role] : [],
      });
    }
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    setUser(null);
    setTenantId(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      tenantId,
      isAuthenticated: user !== null,
      isLoading,
      login,
      logout,
    }),
    [user, tenantId, isLoading, login, logout],
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
