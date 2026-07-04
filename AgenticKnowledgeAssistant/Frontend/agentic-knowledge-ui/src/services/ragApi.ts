import { ApiResponse, RagDocument, RagDocumentDto, RagSearchResultDto } from '../models/api';
import { httpClient } from './httpClient';

export const ragApi = {
  async upload(file: File): Promise<{ documentId: number; fileName: string; status: string; chunkCount: number }> {
    const form = new FormData();
    form.append('file', file);
    const { data } = await httpClient.post<ApiResponse<any>>('/rag/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    const payload = data.data ?? data.Data;
    return {
      documentId: payload.DocumentId,
      fileName: payload.FileName,
      status: payload.Status,
      chunkCount: payload.ChunkCount
    };
  },

  async search(query: string, topK = 8): Promise<RagSearchResultDto[]> {
    const { data } = await httpClient.post<ApiResponse<{ Results: RagSearchResultDto[] }>>('/rag/search', { query, topK });
    return (data.data ?? data.Data)?.Results ?? [];
  },

  async chat(question: string, topK = 8): Promise<{ answer: string; sources: RagSearchResultDto[]; confidenceScore: number }> {
    const { data } = await httpClient.post<ApiResponse<any>>('/rag/chat', { question, topK });
    const payload = data.data ?? data.Data;
    return {
      answer: payload.Answer,
      sources: payload.Sources ?? [],
      confidenceScore: payload.ConfidenceScore ?? 0
    };
  },

  async getDocument(id: number): Promise<RagDocument> {
    const { data } = await httpClient.get<ApiResponse<RagDocumentDto>>(`/rag/document/${id}`);
    return mapDocument(data.data ?? data.Data);
  },

  async deleteDocument(id: number): Promise<void> {
    await httpClient.delete(`/rag/document/${id}`);
  }
};

function mapDocument(dto?: RagDocumentDto): RagDocument {
  if (!dto) throw new Error('Document response was empty');
  return {
    documentId: dto.DocumentId,
    fileName: dto.FileName,
    title: dto.Title,
    processingStatus: dto.ProcessingStatus,
    chunkCount: dto.ChunkCount,
    embeddingCount: dto.EmbeddingCount,
    summary: dto.Summary,
    createdDate: dto.CreatedDate,
    updatedDate: dto.UpdatedDate
  };
}
