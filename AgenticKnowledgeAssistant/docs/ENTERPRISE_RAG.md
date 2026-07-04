# Enterprise RAG

## Current Architecture Review

- Upload: existing `/api/document/upload` supports PDF, DOCX, and TXT through `DocumentBAL`; it saves legacy documents and one document-level embedding.
- OCR/Vision: `AgentBAL` uses `IOcrService` and `IImageContextService` for image and scanned attachment context.
- Chat: `ChatBAL` now wraps the existing `AgentBAL` with Conversation Memory and Long-Term Memory.
- Search: existing document search is keyword/stored-procedure based with optional OpenAI embedding search.
- Authentication: controllers use JWT authorization and the `uid` claim.
- Extension point selected: new `/api/rag/*` module with independent SQL tables and services.

## RAG Architecture Diagram

```text
React Enterprise RAG Page
  | /api/rag/upload, /api/rag/search, /api/rag/chat
  v
RagController
  v
RagService
  |-- File validation / virus-scan hook
  |-- Text extraction
  |-- Metadata + heading detection
  |-- Paragraph + sliding-window chunking
  |-- IAgentBAL.GenerateEmbeddingAsync
  |-- Hybrid search + re-ranking
  |-- IAgentBAL.GenerateResponseAsync
  v
IRagRepository
  v
SQL Server Vector Store Tables
```

## Folder Structure

- `Application/DTOs/Models/RagModels.cs`
- `Application/DTOs/RequestDTOs/RagRequestDTOs.cs`
- `Application/DTOs/ResponseDTOs/RagResponseDTOs.cs`
- `Application/Interfaces/DataAccess/IRagRepository.cs`
- `Application/Services/Interfaces/IRagService.cs`
- `Application/Services/RagService.cs`
- `Infrastructure/DataAccess/RagRepository.cs`
- `API/Controllers/RagController.cs`
- `Frontend/src/pages/EnterpriseRagPage.tsx`
- `Frontend/src/services/ragApi.ts`

## Database Design

New tables only:

- `tblAI_RagDocuments`
- `tblAI_RagChunks`
- `tblAI_RagEmbeddings`
- `tblAI_RagVectorIndex`
- `tblAI_RagChunkMetadata`
- `tblAI_RagSearchHistory`

Scripts:

- Deploy: `Database/Scripts/040_EnterpriseRag.sql`
- Rollback: `Database/Scripts/040_Rollback_EnterpriseRag.sql`

## APIs

- `POST /api/rag/upload`
- `POST /api/rag/index`
- `POST /api/rag/search`
- `POST /api/rag/chat`
- `GET /api/rag/document/{id}`
- `DELETE /api/rag/document/{id}`

## Vector Store

The first provider is SQL Server JSON-vector storage behind `IRagRepository`. The service-level boundary supports future provider implementations for Azure AI Search, Qdrant, Pinecone, ChromaDB, Milvus, and pgvector without changing API contracts.

## Hybrid Ranking

- Keyword score from chunk text token matches.
- Vector score from embedding cosine similarity.
- Hybrid score = normalized keyword score `45%` + vector score `55%`.
- Top chunks are re-ranked and passed to the grounded response prompt.

## Security

- JWT required on all RAG APIs.
- User isolation enforced with `UserId` in every repository query.
- File type and size validation are enforced.
- Prompt asks the LLM to answer only from retrieved chunks and cite sources.
- Virus scanning is represented as an explicit pipeline hook; plug in Defender, ClamAV, or enterprise AV before extraction in production.

## Performance

- Chunking limits context size.
- Embeddings are generated per chunk and stored for reuse.
- SQL indexes target user document lookup, chunk retrieval, embeddings, and search history.
- Next production steps: background indexing via Hangfire, Redis chunk cache, batch embeddings, streaming RAG responses, and external vector store.

## Deployment Steps

1. Deploy API and UI.
2. Run `Database/Scripts/040_EnterpriseRag.sql`.
3. Configure embedding provider in `appsettings.json` or local provider settings.
4. Restart API.
5. Upload a document from `/enterprise-rag`.
6. Validate `/api/rag/search` and `/api/rag/chat` return sources.

## Testing Strategy

- Unit test extraction, chunking, scoring, and grounded fallback answer generation.
- Integration test RAG upload/search/chat against SQL Server.
- Security test cross-user document access.
- Regression test existing Chat, OCR, Vision AI, Conversation Memory, Long-Term Memory, and existing Document Upload.
