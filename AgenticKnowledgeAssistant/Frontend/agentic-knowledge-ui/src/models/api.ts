export type ApiStatus = {
  name: string;
  version: string;
  status: string;
  timestamp: string;
};

export type ApiResponse<T> = {
  ReturnCode: number;
  ReturnMessage: string;
  ResponseTime?: string;
  Data?: T;
  data?: T;
  success?: boolean;
  message?: string;
  totalCount?: number;
  Headers?: unknown;
};

export type ChatRequest = {
  question: string;
  mode?: string;
  sessionGuid?: string | null;
  attachmentBase64?: string | null;
  attachmentName?: string | null;
  attachments?: ChatAttachment[];
  targetLanguage?: string | null;
};

export type ChatAttachment = {
  fileName: string;
  contentType: string;
  base64Content: string;
  size: number;
};

export type ChatResponse = {
  answer: string;
  sessionGuid?: string | null;
  sources: string[];
  toolUsed: string;
  confidenceScore?: number;
  responseTimeMs?: number;
  promptTokens?: number;
  completionTokens?: number;
  totalTokens?: number;
  detectedLanguage?: string;
  translatedAnswer?: string;
};

export type ChatResponseDto = {
  Answer: string;
  SessionGuid?: string | null;
  Sources?: string[];
  ToolUsed: string;
  ConfidenceScore?: number;
  ResponseTimeMs?: number;
  PromptTokens?: number;
  CompletionTokens?: number;
  TotalTokens?: number;
  DetectedLanguage?: string;
  TranslatedAnswer?: string;
};

export type ChatMessage = {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
  sources?: string[];
  toolUsed?: string;
  elapsedMs?: number;
  isError?: boolean;
  confidenceScore?: number;
  promptTokens?: number;
  completionTokens?: number;
  totalTokens?: number;
  detectedLanguage?: string;
  translatedAnswer?: string;
  attachmentName?: string;
  attachmentBase64?: string;
  attachments?: ChatAttachment[];
};

export type ConversationSession = {
  id: number;
  sessionGuid: string;
  userId: number;
  title: string;
  status: string;
  isPinned: boolean;
  isFavorite: boolean;
  createdDate: string;
  updatedDate: string;
  messageCount: number;
  lastMessagePreview?: string | null;
};

export type ConversationSessionDto = {
  Id: number;
  SessionGuid: string;
  UserId: number;
  Title: string;
  Status: string;
  IsPinned: boolean;
  IsFavorite: boolean;
  CreatedDate: string;
  UpdatedDate: string;
  MessageCount: number;
  LastMessagePreview?: string | null;
};

export type ConversationMessageDto = {
  MessageId: number;
  SessionGuid: string;
  UserId: number;
  Role: 'User' | 'Assistant' | 'System';
  Message: string;
  Tokens?: number;
  CreatedDate: string;
  Metadata?: string | null;
};

export type MemoryCategory = {
  categoryId: number;
  categoryName: string;
  description: string;
};

export type MemoryCategoryDto = {
  CategoryId: number;
  CategoryName: string;
  Description: string;
};

export type UserMemory = {
  memoryId: number;
  userId: number;
  category: string;
  key: string;
  value: string;
  isActive: boolean;
  isPinned: boolean;
  isFavorite: boolean;
  createdDate: string;
  updatedDate: string;
  metadata?: string | null;
};

export type UserMemoryDto = {
  MemoryId: number;
  UserId: number;
  Category: string;
  Key: string;
  Value: string;
  IsActive: boolean;
  IsPinned: boolean;
  IsFavorite: boolean;
  CreatedDate: string;
  UpdatedDate: string;
  Metadata?: string | null;
};

export type RagDocument = {
  documentId: number;
  fileName: string;
  title: string;
  processingStatus: string;
  chunkCount: number;
  embeddingCount: number;
  summary?: string | null;
  createdDate: string;
  updatedDate: string;
};

export type RagDocumentDto = {
  DocumentId: number;
  FileName: string;
  Title: string;
  ProcessingStatus: string;
  ChunkCount: number;
  EmbeddingCount: number;
  Summary?: string | null;
  CreatedDate: string;
  UpdatedDate: string;
};

export type RagSearchResultDto = {
  DocumentId: number;
  ChunkId: number;
  FileName: string;
  Title: string;
  ChunkIndex: number;
  PageNumber?: number | null;
  Section: string;
  Heading: string;
  Content: string;
  KeywordScore: number;
  VectorScore: number;
  HybridScore: number;
};

export type DocumentSummary = {
  id: number;
  title: string;
  preview: string;
  createdDate: string;
};

export type DocumentSummaryDto = {
  Id: number;
  Title: string;
  Preview: string;
  CreatedDate: string;
};

export type UploadResponse = {
  message: string;
  fileName: string;
  documentId?: number;
};

export type UploadResponseDto = {
  message?: string;
  fileName?: string;
  documentId?: number;
};

export type KnowledgeItem = {
  id: number;
  title: string;
  preview: string;
  createdDate: string;
  sourceType: 'Document' | 'Manual' | 'System';
};

export type ChatHistoryItem = {
  id: number;
  question: string;
  answer: string;
  createdAt: string;
  toolUsed?: string;
};

export type UserDetails = {
  id: number;
  userName: string;
  email: string;
  fullName: string;
  mobileNumber: string;
  isActive: boolean;
};

export type LoginRequest = {
  email: string;
  password: string;
  rememberMe: boolean;
};

export type RegisterRequest = {
  fullName: string;
  email: string;
  mobileNumber: string;
  password: string;
  confirmPassword: string;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
  tokenType: string;
  user: UserDetails;
  roles: string[];
  permissions: string[];
  isMfaRequired?: boolean;
  mfaToken?: string;
};

export type LoginResponseDto = {
  AccessToken: string;
  RefreshToken: string;
  ExpiresAtUtc: string;
  RefreshTokenExpiresAtUtc: string;
  TokenType: string;
  User: {
    Id: number;
    UserName: string;
    Email: string;
    FullName: string;
    MobileNumber: string;
    IsActive: boolean;
  };
  Roles: string[];
  Permissions?: string[];
  IsMfaRequired?: boolean;
  MfaToken?: string;
};

export type AdminUser = {
  id: number;
  userName: string;
  fullName: string;
  email: string;
  mobileNumber: string;
  roleId?: number;
  roleName: string;
  isActive: boolean;
  createdDate: string;
  lastLoginDate?: string;
};

export type AdminUserDto = {
  Id: number;
  UserName: string;
  FullName: string;
  Email: string;
  MobileNumber: string;
  RoleId?: number;
  RoleName: string;
  IsActive: boolean;
  CreatedDate: string;
  LastLoginDate?: string;
};

export type SaveUserRequest = {
  userName: string;
  fullName: string;
  email: string;
  mobileNumber: string;
  password?: string;
  confirmPassword?: string;
  roleId: number;
  isActive: boolean;
};

export type AdminRole = {
  id: number;
  roleName: string;
  description: string;
  isSystemRole: boolean;
  isActive: boolean;
  createdDate: string;
};

export type AdminRoleDto = {
  Id: number;
  RoleName: string;
  Description: string;
  IsSystemRole: boolean;
  IsActive: boolean;
  CreatedDate: string;
};

export type SaveRoleRequest = {
  roleName: string;
  description: string;
  isActive: boolean;
};

export type Permission = {
  id: number;
  permissionName: string;
  description: string;
  isAssigned: boolean;
  isActive: boolean;
};

export type PermissionDto = {
  Id: number;
  PermissionName: string;
  Description: string;
  IsAssigned: boolean;
  IsActive: boolean;
};
