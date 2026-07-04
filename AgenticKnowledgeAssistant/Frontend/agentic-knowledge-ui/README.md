# Agentic Knowledge Assistant UI

Production-ready React 18 + TypeScript + Vite frontend for the .NET 8 Agentic Knowledge Assistant API.

## Stack

- React 18, TypeScript, Vite
- Material UI with responsive desktop, tablet, and mobile layouts
- Axios API layer with environment-based base URL
- React Router navigation
- React Toastify notifications
- Markdown rendering with GitHub-flavored Markdown and code highlighting
- Drag-and-drop document upload with progress feedback
- Light and dark theme support

## Folder Structure

```text
src/
  components/
    chat/
    common/
    layout/
    upload/
  config/
  contexts/
  models/
  pages/
  services/
  theme/
  App.tsx
  main.tsx
```

## Pages

- Login Page
- Dashboard
- Chat Assistant Page
- Document Upload Page
- Knowledge Base Page
- Chat History Page
- Settings Page

## API Configuration

Create `.env.local` from `.env.example`:

```bash
cp .env.example .env.local
```

```env
VITE_API_BASE_URL=https://localhost:5243/api
VITE_APP_NAME=Agentic Knowledge Assistant
```

## Installation

```bash
cd Frontend/agentic-knowledge-ui
npm install
```

## Development

```bash
npm run dev
```

The UI runs at `http://localhost:5173` and proxies `/api` requests to `https://localhost:5243`.

## Production Build

```bash
npm run build
npm run preview
```

The production output is generated in `dist/`.

## API Services

- `chatApi.ts`: `POST /api/chat`
- `documentApi.ts`: `GET /api/document`, `GET /api/document/search`, `POST /api/document/upload`, `DELETE /api/document/{id}`
- `knowledgeBaseApi.ts`: document-backed knowledge base abstraction
- `historyApi.ts`: local storage history abstraction, ready to swap for a server endpoint
- `statusApi.ts`: `GET /api/status`, `GET /api/health`
