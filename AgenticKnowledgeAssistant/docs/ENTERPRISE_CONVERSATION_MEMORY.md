# Enterprise Conversation Memory

## Architecture Diagram

```text
React Chat UI
  | /api/chat, /api/chat/session, /api/chat/history
  v
ChatController
  | authenticated user id from JWT uid claim
  v
ChatBAL
  |-- ConversationMemoryService
  |     |-- ConversationRepository
  |     |-- SQL Server: tblAI_ConversationSessions, tblAI_ConversationMessages
  |
  |-- AgentBAL
        |-- Existing SmartAuto / RAG / OCR / Vision / Database / Provider flow
```

## Database Design

- `tblAI_ConversationSessions`: one row per user-owned conversation.
- `tblAI_ConversationMessages`: append-only user, assistant, and system messages for a session.
- User isolation is enforced by every repository query filtering by `UserId` and `SessionGuid`.
- Deployment script: `Database/Scripts/020_EnterpriseConversationMemory.sql`.
- Rollback script: `Database/Scripts/020_Rollback_EnterpriseConversationMemory.sql`.

## Backend Folder Structure

- `Application/DTOs/Models/ConversationModels.cs`
- `Application/DTOs/RequestDTOs/ConversationRequestDTOs.cs`
- `Application/Interfaces/DataAccess/IConversationRepository.cs`
- `Application/Services/Interfaces/IConversationMemoryService.cs`
- `Application/Services/ConversationMemoryService.cs`
- `Infrastructure/DataAccess/ConversationRepository.cs`
- `API/Controllers/ChatController.cs`

## APIs

- `POST /api/chat/session`: create a session.
- `GET /api/chat/session/{id}`: load a session.
- `GET /api/chat/sessions`: search/list sessions with pagination filters.
- `PATCH /api/chat/session/{id}`: rename, pin, favorite, archive.
- `POST /api/chat/message`: save a message manually.
- `GET /api/chat/history/{sessionId}`: load paged messages.
- `DELETE /api/chat/session/{id}`: soft-delete a session.
- Existing `POST /api/chat` remains unchanged for callers and now returns `SessionGuid`.

## Conversation Flow

1. Resolve or create conversation session.
2. Load recent messages for the authenticated user/session.
3. Build memory context with system guidance, summary, current mode/language, attachments, and sliding-window messages.
4. Call the existing `AgentBAL` flow.
5. Save the original user message and assistant response.
6. Save legacy flat chat history for backward compatibility.

## Token Management

- Uses lightweight token estimation (`characters / 4`).
- Keeps a configurable recent-message window.
- Adds a compact summary of the last user request and assistant response.
- Trims context before invoking the existing AI/RAG/OCR/Vision pipeline.

## Frontend

- Chat UI now has a conversation rail.
- Supports New Chat, Continue Conversation, Rename, Delete, Search, Pin, Favorite, active conversation highlighting, and timestamps/previews.
- Existing chat composer, attachments, OCR, Vision, RAG, and history posting remain intact.

## Security

- All new endpoints require authorization.
- Repository queries always include `UserId`.
- Cross-user `SessionGuid` access returns no data.

## Logging

- Logs conversation creation, context building, message save, and memory failures.
- Memory failures are non-blocking for `/api/chat` so existing chat functionality is preserved if the schema has not been deployed.

## Deployment Steps

1. Deploy backend and frontend.
2. Run `Database/Scripts/020_EnterpriseConversationMemory.sql`.
3. Restart API instances.
4. Validate `/api/chat/session`, `/api/chat/sessions`, and `/api/chat`.
5. Confirm follow-up prompts resolve references within the same `SessionGuid`.

## Testing Strategy

- Unit test `ConversationMemoryService` for context trimming, role normalization, title generation, and save exchange behavior.
- Integration test repository CRUD with SQL Server using two different users to verify isolation.
- API test session create/list/history/delete with JWT.
- UI test new chat, load conversation, rename, pin/favorite, delete, and follow-up prompt continuity.

## Build Validation

- Backend: `dotnet build AgenticKnowledgeAssistant\AgenticKnowledgeAssistant.sln`
- Frontend: `npm run build` from `Frontend/agentic-knowledge-ui`
