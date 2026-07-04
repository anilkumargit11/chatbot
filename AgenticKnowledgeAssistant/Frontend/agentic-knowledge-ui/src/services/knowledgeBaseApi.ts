import { KnowledgeItem } from '../models/api';
import { documentApi } from './documentApi';

export const knowledgeBaseApi = {
  async list(): Promise<KnowledgeItem[]> {
    const documents = await documentApi.list();
    return documents.map((document) => ({
      ...document,
      sourceType: 'Document'
    }));
  },

  async search(query: string): Promise<KnowledgeItem[]> {
    const documents = await documentApi.search(query);
    return documents.map((document) => ({
      ...document,
      sourceType: 'Document'
    }));
  }
};
