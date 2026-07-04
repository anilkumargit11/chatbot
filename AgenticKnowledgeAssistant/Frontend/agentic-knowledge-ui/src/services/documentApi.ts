import { ApiResponse, DocumentSummary, DocumentSummaryDto, UploadResponse, UploadResponseDto } from '../models/api';
import { httpClient } from './httpClient';

function mapDocument(document: DocumentSummaryDto): DocumentSummary {
  return {
    id: document.Id,
    title: document.Title,
    preview: document.Preview,
    createdDate: document.CreatedDate
  };
}

export const documentApi = {
  async list(): Promise<DocumentSummary[]> {
    const { data } = await httpClient.get<ApiResponse<DocumentSummaryDto[]>>('/document');
    return (data.data ?? data.Data ?? []).map(mapDocument);
  },

  async search(query: string): Promise<DocumentSummary[]> {
    const { data } = await httpClient.get<ApiResponse<DocumentSummaryDto[]>>('/document/search', {
      params: { q: query }
    });
    return (data.data ?? data.Data ?? []).map(mapDocument);
  },

  async upload(file: File, onUploadProgress?: (progress: number) => void): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const { data } = await httpClient.post<ApiResponse<UploadResponseDto>>('/document/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (event) => {
        if (!event.total || !onUploadProgress) {
          return;
        }

        onUploadProgress(Math.round((event.loaded * 100) / event.total));
      }
    });

    return {
      message: data.data?.message ?? data.Data?.message ?? data.message ?? data.ReturnMessage,
      fileName: data.data?.fileName ?? data.Data?.fileName ?? file.name,
      documentId: data.data?.documentId ?? data.Data?.documentId
    };
  },

  async remove(id: number): Promise<void> {
    await httpClient.delete(`/document/${id}`);
  }
};
