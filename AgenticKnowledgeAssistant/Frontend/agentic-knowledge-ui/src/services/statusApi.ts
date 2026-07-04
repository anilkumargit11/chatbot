import { ApiResponse, ApiStatus } from '../models/api';
import { httpClient } from './httpClient';

type ApiStatusDto = {
  Name: string;
  Version: string;
  Status: string;
  Timestamp: string;
};

export const statusApi = {
  async getStatus(): Promise<ApiStatus> {
    const { data } = await httpClient.get<ApiResponse<ApiStatusDto>>('/status');
    const status = data.data ?? data.Data;

    if (!status) {
      throw new Error(data.ReturnMessage || 'Unable to load API status');
    }

    return {
      name: status.Name,
      version: status.Version,
      status: status.Status,
      timestamp: status.Timestamp
    };
  },

  async getHealth(): Promise<{ status: string; timestamp: string }> {
    const { data } = await httpClient.get<{ status: string; timestamp: string }>('/health');
    return data;
  }
};
