import type { LoginResponse } from '../models/api';

const authKey = 'aka.auth';

export function getStoredAuth(): LoginResponse | null {
  const value = localStorage.getItem(authKey) ?? sessionStorage.getItem(authKey);
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as LoginResponse;
  } catch {
    clearStoredAuth();
    return null;
  }
}

export function setStoredAuth(auth: LoginResponse, rememberMe: boolean) {
  clearStoredAuth();
  const value = JSON.stringify(auth);
  if (rememberMe) {
    localStorage.setItem(authKey, value);
  } else {
    sessionStorage.setItem(authKey, value);
  }
}

export function clearStoredAuth() {
  localStorage.removeItem(authKey);
  sessionStorage.removeItem(authKey);
}

export function isTokenExpired(auth: LoginResponse | null) {
  if (!auth?.accessToken || !auth.expiresAtUtc) {
    return true;
  }

  return new Date(auth.expiresAtUtc).getTime() <= Date.now();
}

export function getTokenExpiry(auth: LoginResponse | null) {
  return auth?.expiresAtUtc ? new Date(auth.expiresAtUtc).getTime() : 0;
}
