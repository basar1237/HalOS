// Auth context — gerçek Identity akışı (Gateway üzerinden). Login → token sakla + /me ile kullanıcı;
// açılışta token varsa oturum kurtarma. ADR-009.

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { api } from './api';
import { clearTokens, getAccessToken, saveTokens } from './token';
import type {
  AuthenticationResult,
  CurrentUserDto,
  LoginCredentials,
  User,
} from '@/shared/types';

interface AuthContextValue {
  user: User | null;
  tenantId: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

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

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const token = await getAccessToken();
      if (!token) {
        if (!cancelled) setIsLoading(false);
        return;
      }
      try {
        const me = await api.get<CurrentUserDto>('/api/identity/me');
        if (!cancelled) {
          setUser(toUser(me));
          setTenantId(me.tenantId);
        }
      } catch {
        await clearTokens();
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (credentials: LoginCredentials) => {
    const result = await api.post<AuthenticationResult>(
      '/api/identity/auth/login',
      credentials,
    );
    await saveTokens(result.accessToken, result.refreshToken);
    setTenantId(result.tenantId);
    try {
      const me = await api.get<CurrentUserDto>('/api/identity/me');
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

  const logout = useCallback(async () => {
    await clearTokens();
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
  const ctx = useContext(AuthContext);
  if (ctx === undefined) {
    throw new Error('useAuth, AuthProvider içinde kullanılmalıdır.');
  }
  return ctx;
}
