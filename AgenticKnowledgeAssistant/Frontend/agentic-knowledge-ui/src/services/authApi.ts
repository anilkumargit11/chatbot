import type { ApiResponse, LoginRequest, LoginResponse, LoginResponseDto, RegisterRequest } from '../models/api';
import { httpClient } from './httpClient';

function normalizeLogin(dto: LoginResponseDto): LoginResponse {
  const raw = dto as LoginResponseDto & Record<string, any>;
  const user = (raw.User ?? raw.user) as Record<string, any> | undefined;

  return {
    accessToken: raw.AccessToken ?? raw.accessToken ?? '',
    refreshToken: raw.RefreshToken ?? raw.refreshToken ?? '',
    expiresAtUtc: raw.ExpiresAtUtc ?? raw.expiresAtUtc ?? '',
    refreshTokenExpiresAtUtc: raw.RefreshTokenExpiresAtUtc ?? raw.refreshTokenExpiresAtUtc ?? '',
    tokenType: raw.TokenType ?? raw.tokenType ?? '',
    user: user ? {
      id: user.Id ?? user.id ?? 0,
      userName: user.UserName ?? user.userName ?? '',
      email: user.Email ?? user.email ?? '',
      fullName: user.FullName ?? user.fullName ?? '',
      mobileNumber: user.MobileNumber ?? user.mobileNumber ?? '',
      isActive: user.IsActive ?? user.isActive ?? false
    } : {
      id: 0,
      userName: '',
      email: '',
      fullName: '',
      mobileNumber: '',
      isActive: false
    },
    roles: raw.Roles ?? raw.roles ?? [],
    permissions: raw.Permissions ?? raw.permissions ?? [],
    isMfaRequired: raw.IsMfaRequired ?? raw.isMfaRequired ?? false,
    mfaToken: raw.MfaToken ?? raw.mfaToken ?? ''
  };
}

function getPayload<T>(response: ApiResponse<T>): T {
  const payload = response.data ?? response.Data;
  if (!payload) {
    throw new Error(response.message || response.ReturnMessage || 'Authentication request failed');
  }

  return payload;
}

export const authApi = {
  async login(request: LoginRequest) {
    const response = await httpClient.post<ApiResponse<LoginResponseDto>>('/auth/login', {
      Email: request.email,
      UserName: request.email,
      Password: request.password,
      RememberMe: request.rememberMe
    });
    return normalizeLogin(getPayload(response.data));
  },

  async verifyMfaLogin(mfaToken: string, code: string, rememberMe: boolean) {
    const response = await httpClient.post<ApiResponse<LoginResponseDto>>('/mfa/verify-login', {
      MfaToken: mfaToken,
      Code: code,
      RememberMe: rememberMe
    });
    return normalizeLogin(getPayload(response.data));
  },

  async register(request: RegisterRequest) {
    const response = await httpClient.post<ApiResponse<{ UserId: number }>>('/auth/register', {
      FullName: request.fullName,
      Email: request.email,
      MobileNumber: request.mobileNumber,
      Password: request.password,
      ConfirmPassword: request.confirmPassword
    });
    return response.data;
  },

  async refresh(refreshToken: string) {
    const response = await httpClient.post<ApiResponse<LoginResponseDto>>('/auth/refresh-token', {
      RefreshToken: refreshToken
    });
    return normalizeLogin(getPayload(response.data));
  },

  async logout(refreshToken: string) {
    await httpClient.post('/auth/logout', { RefreshToken: refreshToken });
  }
};
