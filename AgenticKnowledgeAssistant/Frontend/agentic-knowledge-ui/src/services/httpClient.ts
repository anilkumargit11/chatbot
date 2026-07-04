import axios, { AxiosError } from 'axios';
import { env } from '../config/env';
import { clearStoredAuth, getStoredAuth } from './authStorage';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 60000,
  headers: {
    'Content-Type': 'application/json'
  }
});

httpClient.interceptors.request.use((config) => {
  const auth = getStoredAuth();
  if (auth?.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`;
  }

  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ error?: string; message?: string }>) => {
    if (error.response?.status === 401) {
      clearStoredAuth();
      if (window.location.pathname !== '/login') {
        window.location.assign('/login');
      }
    }

    const message =
      error.response?.data?.error ??
      error.response?.data?.message ??
      (error.response?.data as { ReturnMessage?: string } | undefined)?.ReturnMessage ??
      error.message ??
      'Request failed. Please try again.';

    return Promise.reject(new Error(message));
  }
);
