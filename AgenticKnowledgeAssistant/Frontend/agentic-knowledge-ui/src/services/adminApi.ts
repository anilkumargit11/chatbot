import {
  AdminRole,
  AdminRoleDto,
  AdminUser,
  AdminUserDto,
  ApiResponse,
  Permission,
  PermissionDto,
  SaveRoleRequest,
  SaveUserRequest
} from '../models/api';
import { httpClient } from './httpClient';

function mapUser(user: AdminUserDto): AdminUser {
  return {
    id: user.Id,
    userName: user.UserName,
    fullName: user.FullName,
    email: user.Email,
    mobileNumber: user.MobileNumber,
    roleId: user.RoleId,
    roleName: user.RoleName,
    isActive: user.IsActive,
    createdDate: user.CreatedDate,
    lastLoginDate: user.LastLoginDate
  };
}

function mapRole(role: AdminRoleDto): AdminRole {
  return {
    id: role.Id,
    roleName: role.RoleName,
    description: role.Description,
    isSystemRole: role.IsSystemRole,
    isActive: role.IsActive,
    createdDate: role.CreatedDate
  };
}

function mapPermission(permission: PermissionDto): Permission {
  return {
    id: permission.Id,
    permissionName: permission.PermissionName,
    description: permission.Description,
    isAssigned: permission.IsAssigned,
    isActive: permission.IsActive
  };
}

export const usersApi = {
  async list(params?: { search?: string; roleId?: number; isActive?: boolean }): Promise<AdminUser[]> {
    const { data } = await httpClient.get<ApiResponse<AdminUserDto[]>>('/users', { params });
    return (data.data ?? data.Data ?? []).map(mapUser);
  },

  async create(request: SaveUserRequest): Promise<void> {
    await httpClient.post('/users', request);
  },

  async update(id: number, request: SaveUserRequest): Promise<void> {
    await httpClient.put(`/users/${id}`, request);
  },

  async remove(id: number): Promise<void> {
    await httpClient.delete(`/users/${id}`);
  },

  async activate(id: number): Promise<void> {
    await httpClient.put(`/users/${id}/activate`);
  },

  async deactivate(id: number): Promise<void> {
    await httpClient.put(`/users/${id}/deactivate`);
  },

  async resetPassword(id: number, password: string, confirmPassword: string): Promise<void> {
    await httpClient.put(`/users/${id}/reset-password`, { password, confirmPassword });
  }
};

export const rolesApi = {
  async list(params?: { search?: string; isActive?: boolean }): Promise<AdminRole[]> {
    const { data } = await httpClient.get<ApiResponse<AdminRoleDto[]>>('/roles', { params });
    return (data.data ?? data.Data ?? []).map(mapRole);
  },

  async create(request: SaveRoleRequest): Promise<void> {
    await httpClient.post('/roles', request);
  },

  async update(id: number, request: SaveRoleRequest): Promise<void> {
    await httpClient.put(`/roles/${id}`, request);
  },

  async remove(id: number): Promise<void> {
    await httpClient.delete(`/roles/${id}`);
  },

  async permissions(roleId: number): Promise<Permission[]> {
    const { data } = await httpClient.get<ApiResponse<PermissionDto[]>>(`/roles/${roleId}/permissions`);
    return (data.data ?? data.Data ?? []).map(mapPermission);
  },

  async assignPermissions(roleId: number, permissionIds: number[]): Promise<void> {
    await httpClient.put(`/roles/${roleId}/permissions`, { permissionIds });
  }
};
