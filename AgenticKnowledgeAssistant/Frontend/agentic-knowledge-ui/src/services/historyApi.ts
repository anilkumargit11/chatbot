import { ChatHistoryItem } from '../models/api';

const STORAGE_KEY = 'agentic-knowledge-chat-history';

export const historyApi = {
  async list(): Promise<ChatHistoryItem[]> {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as ChatHistoryItem[]) : [];
  },

  async add(item: Omit<ChatHistoryItem, 'id'>): Promise<ChatHistoryItem> {
    const items = await this.list();
    const nextItem = { ...item, id: Date.now() };
    localStorage.setItem(STORAGE_KEY, JSON.stringify([nextItem, ...items].slice(0, 100)));
    return nextItem;
  },

  async clear(): Promise<void> {
    localStorage.removeItem(STORAGE_KEY);
  }
};
