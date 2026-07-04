import { ApiResponse, MemoryCategory, MemoryCategoryDto, UserMemory, UserMemoryDto } from '../models/api';
import { httpClient } from './httpClient';

export const memoryApi = {
  async categories(): Promise<MemoryCategory[]> {
    const { data } = await httpClient.get<ApiResponse<MemoryCategoryDto[]>>('/memory/categories');
    return (data.data ?? data.Data ?? []).map(mapCategory);
  },

  async list(params?: {
    search?: string;
    category?: string;
    isActive?: boolean;
    isPinned?: boolean;
    isFavorite?: boolean;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<UserMemory[]> {
    const { data } = await httpClient.get<ApiResponse<UserMemoryDto[]>>('/memory', { params });
    return (data.data ?? data.Data ?? []).map(mapMemory);
  },

  async save(request: {
    category: string;
    key: string;
    value: string;
    isPinned?: boolean;
    isFavorite?: boolean;
    metadata?: string | null;
  }): Promise<UserMemory> {
    const { data } = await httpClient.post<ApiResponse<UserMemoryDto>>('/memory', request);
    return mapMemory(data.data ?? data.Data);
  },

  async update(memoryId: number, request: {
    category?: string;
    key?: string;
    value?: string;
    isActive?: boolean;
    isPinned?: boolean;
    isFavorite?: boolean;
    metadata?: string | null;
  }): Promise<void> {
    await httpClient.put(`/memory/${memoryId}`, request);
  },

  async remove(memoryId: number): Promise<void> {
    await httpClient.delete(`/memory/${memoryId}`);
  }
};

function mapCategory(dto: MemoryCategoryDto): MemoryCategory {
  return {
    categoryId: dto.CategoryId,
    categoryName: dto.CategoryName,
    description: dto.Description
  };
}

function mapMemory(dto?: UserMemoryDto): UserMemory {
  if (!dto) {
    throw new Error('Memory response was empty');
  }

  return {
    memoryId: dto.MemoryId,
    userId: dto.UserId,
    category: dto.Category,
    key: dto.Key,
    value: dto.Value,
    isActive: dto.IsActive,
    isPinned: dto.IsPinned,
    isFavorite: dto.IsFavorite,
    createdDate: dto.CreatedDate,
    updatedDate: dto.UpdatedDate,
    metadata: dto.Metadata
  };
}
