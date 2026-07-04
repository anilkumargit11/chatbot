import { ApiResponse, ChatRequest, ChatResponse, ChatResponseDto, ConversationMessageDto, ConversationSession, ConversationSessionDto } from '../models/api';
import { httpClient } from './httpClient';

export const chatApi = {
  async send(params: {
    question: string;
    mode?: string;
    sessionGuid?: string | null;
    attachmentBase64?: string | null;
    attachmentName?: string | null;
    attachments?: ChatRequest['attachments'];
    targetLanguage?: string | null;
  }): Promise<ChatResponse> {
    const firstAttachment = params.attachments?.[0];
    const { data } = await httpClient.post<ApiResponse<ChatResponseDto>>('/chat', {
      question: params.question,
      mode: params.mode ?? 'SmartAuto',
      sessionGuid: params.sessionGuid,
      attachmentBase64: params.attachmentBase64 ?? firstAttachment?.base64Content,
      attachmentName: params.attachmentName ?? firstAttachment?.fileName,
      attachments: params.attachments,
      targetLanguage: params.targetLanguage
    } as unknown as ChatRequest);

    const payload = data.data ?? data.Data;
    if (!payload) {
      throw new Error(data.message || data.ReturnMessage || 'Chat request failed');
    }

    return {
      answer: read(payload, 'Answer', 'answer') ?? '',
      sessionGuid: read(payload, 'SessionGuid', 'sessionGuid'),
      sources: read(payload, 'Sources', 'sources') ?? [],
      toolUsed: read(payload, 'ToolUsed', 'toolUsed') ?? 'AgentCore',
      confidenceScore: read(payload, 'ConfidenceScore', 'confidenceScore'),
      responseTimeMs: read(payload, 'ResponseTimeMs', 'responseTimeMs'),
      promptTokens: read(payload, 'PromptTokens', 'promptTokens'),
      completionTokens: read(payload, 'CompletionTokens', 'completionTokens'),
      totalTokens: read(payload, 'TotalTokens', 'totalTokens'),
      detectedLanguage: read(payload, 'DetectedLanguage', 'detectedLanguage'),
      translatedAnswer: read(payload, 'TranslatedAnswer', 'translatedAnswer')
    };
  },

  async createSession(title?: string): Promise<ConversationSession> {
    const { data } = await httpClient.post<ApiResponse<ConversationSessionDto>>('/chat/session', { title });
    return mapSession(data.data ?? data.Data);
  },

  async listSessions(params?: { search?: string; pageNumber?: number; pageSize?: number; pinned?: boolean; favorite?: boolean }): Promise<ConversationSession[]> {
    const { data } = await httpClient.get<ApiResponse<ConversationSessionDto[]>>('/chat/sessions', { params });
    return (data.data ?? data.Data ?? []).map(mapSession);
  },

  async loadMessages(sessionGuid: string): Promise<ConversationMessageDto[]> {
    const { data } = await httpClient.get<ApiResponse<ConversationMessageDto[]>>(`/chat/history/${sessionGuid}`, {
      params: { skip: 0, take: 100 }
    });
    return data.data ?? data.Data ?? [];
  },

  async updateSession(sessionGuid: string, request: { title?: string; isPinned?: boolean; isFavorite?: boolean; status?: string }): Promise<void> {
    await httpClient.patch(`/chat/session/${sessionGuid}`, request);
  },

  async deleteSession(sessionGuid: string): Promise<void> {
    await httpClient.delete(`/chat/session/${sessionGuid}`);
  }
};

function mapSession(dto?: ConversationSessionDto): ConversationSession {
  if (!dto) {
    throw new Error('Conversation response was empty');
  }

  return {
    id: read(dto, 'Id', 'id') ?? 0,
    sessionGuid: read(dto, 'SessionGuid', 'sessionGuid') ?? '',
    userId: read(dto, 'UserId', 'userId') ?? 0,
    title: read(dto, 'Title', 'title') ?? 'New Chat',
    status: read(dto, 'Status', 'status') ?? 'Active',
    isPinned: read(dto, 'IsPinned', 'isPinned') ?? false,
    isFavorite: read(dto, 'IsFavorite', 'isFavorite') ?? false,
    createdDate: read(dto, 'CreatedDate', 'createdDate') ?? new Date().toISOString(),
    updatedDate: read(dto, 'UpdatedDate', 'updatedDate') ?? new Date().toISOString(),
    messageCount: read(dto, 'MessageCount', 'messageCount') ?? 0,
    lastMessagePreview: normalizeLegacyAssistantText(read(dto, 'LastMessagePreview', 'lastMessagePreview'))
  };
}

export function normalizeLegacyAssistantText(message?: string | null): string | null | undefined {
  if (!message) {
    return message;
  }

  return message.trim().toLowerCase() === 'please upload an image or scanned file for ocr analysis.'
    ? 'Legacy OCR routing response hidden. Resend the question to get the corrected answer.'
    : message;
}

function read<T = any>(source: unknown, pascalKey: string, camelKey: string): T | undefined {
  const record = source as Record<string, T> | null | undefined;
  return record?.[pascalKey] ?? record?.[camelKey];
}
