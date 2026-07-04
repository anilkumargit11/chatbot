import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import type { LoginRequest, LoginResponse, RegisterRequest } from '../models/api';
import { authApi } from '../services/authApi';
import { clearStoredAuth, getStoredAuth, isTokenExpired, setStoredAuth } from '../services/authStorage';

type AuthContextValue = {
  auth: LoginResponse | null;
  isAuthenticated: boolean;
  hasRole: (roles: string | string[]) => boolean;
  hasPermission: (permissions: string | string[]) => boolean;
  login: (request: LoginRequest) => Promise<LoginResponse>;
  register: (request: RegisterRequest) => Promise<void>;
  verifyMfa: (mfaToken: string, code: string, rememberMe: boolean) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [auth, setAuth] = useState<LoginResponse | null>(() => getStoredAuth());

  useEffect(() => {
    if (auth && !Array.isArray(auth.permissions)) {
      clearStoredAuth();
      setAuth(null);
      toast.info('Permissions were updated. Please sign in again.');
      navigate('/login', { replace: true });
      return;
    }

    if (auth && isTokenExpired(auth)) {
      clearStoredAuth();
      setAuth(null);
      toast.info('Your session expired. Please sign in again.');
      navigate('/login', { replace: true });
    }
  }, [auth, navigate]);

  useEffect(() => {
    if (!auth?.expiresAtUtc) {
      return undefined;
    }

    const msUntilExpiry = new Date(auth.expiresAtUtc).getTime() - Date.now();
    if (msUntilExpiry <= 0) {
      clearStoredAuth();
      setAuth(null);
      navigate('/login', { replace: true });
      return undefined;
    }

    const timeout = window.setTimeout(() => {
      clearStoredAuth();
      setAuth(null);
      toast.info('Your session expired. Please sign in again.');
      navigate('/login', { replace: true });
    }, msUntilExpiry);

    return () => window.clearTimeout(timeout);
  }, [auth, navigate]);

  const value = useMemo<AuthContextValue>(
    () => ({
      auth,
      isAuthenticated: Boolean(auth && !isTokenExpired(auth) && auth.accessToken),
      hasRole(roles) {
        const requiredRoles = Array.isArray(roles) ? roles : [roles];
        const userRoles = auth?.roles ?? [];
        return requiredRoles.some((role) => userRoles.some((userRole) => userRole.toLowerCase() === role.toLowerCase()));
      },
      hasPermission(permissions) {
        const requiredPermissions = Array.isArray(permissions) ? permissions : [permissions];
        const userPermissions = auth?.permissions ?? [];
        return requiredPermissions.some((permission) => userPermissions.some((userPermission) => userPermission.toLowerCase() === permission.toLowerCase()));
      },
      async login(request) {
        const result = await authApi.login(request);
        if (result.isMfaRequired) {
          return result;
        }
        if (!result.accessToken || !result.expiresAtUtc) {
          throw new Error('Login succeeded, but the server did not return a valid session.');
        }
        setStoredAuth(result, request.rememberMe);
        setAuth(result);
        const stored = getStoredAuth();
        if (!stored || isTokenExpired(stored)) {
          throw new Error('Login session could not be saved. Please try again.');
        }
        toast.success('Login successful');
        window.location.replace('/');
        return result;
      },
      async verifyMfa(mfaToken, code, rememberMe) {
        const result = await authApi.verifyMfaLogin(mfaToken, code, rememberMe);
        if (!result.accessToken || !result.expiresAtUtc) {
          throw new Error('MFA was verified, but the server did not return a valid login session.');
        }
        setStoredAuth(result, rememberMe);
        setAuth(result);
        const stored = getStoredAuth();
        if (!stored || isTokenExpired(stored)) {
          throw new Error('MFA session could not be saved. Please try again.');
        }
        toast.success('Login successful (MFA Verified)');
        window.location.replace('/');
      },
      async register(request) {
        await authApi.register(request);
        toast.success('Registration successful. Please sign in.');
        navigate('/login', { replace: true });
      },
      async logout() {
        const refreshToken = auth?.refreshToken;
        clearStoredAuth();
        setAuth(null);
        if (refreshToken) {
          await authApi.logout(refreshToken).catch(() => undefined);
        }
        navigate('/login', { replace: true });
      }
    }),
    [auth, navigate]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
