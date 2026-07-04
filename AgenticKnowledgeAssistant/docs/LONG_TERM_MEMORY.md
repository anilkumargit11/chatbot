# Long-Term Memory

## Architecture Diagram

```text
React AI Memory Page
  | /api/memory
  v
MemoryController
  | JWT uid claim
  v
LongTermMemoryService
  |-- explicit-memory detector
  |-- sensitive-data validator
  |-- distributed memory cache
  v
MemoryRepository
  v
SQL Server: tblAI_MemoryCategory, tblAI_UserMemory

ChatController -> ChatBAL
  |-- LongTermMemoryService.BuildMemoryContextAsync
  |-- ConversationMemoryService.BuildContextAsync
  v
Existing AgentBAL / RAG / OCR / Vision / AI Provider pipeline
```

## Database Design

- `tblAI_MemoryCategory`: supported memory categories such as User Preferences, Project Memory, Workspace Memory, Favorite Items, Pinned Knowledge, and Reusable Context.
- `tblAI_UserMemory`: user-owned key/value memories with active, pinned, and favorite flags.
- Every read/write query includes `UserId` to enforce owner isolation.

Scripts:

- Deploy: `Database/Scripts/030_LongTermMemory.sql`
- Rollback: `Database/Scripts/030_Rollback_LongTermMemory.sql`

## APIs

- `POST /api/memory`: save memory.
- `GET /api/memory`: list/filter memories.
- `GET /api/memory/search`: search memories.
- `GET /api/memory/categories`: list categories.
- `GET /api/memory/{id}`: get one memory.
- `PUT /api/memory/{id}`: update, enable, disable, pin, favorite.
- `DELETE /api/memory/{id}`: delete memory.

## Chat Behavior

Before calling the existing AI pipeline, `ChatBAL` now loads:

1. Long-Term Memory
2. Conversation Memory
3. Current user message
4. Current attachments, OCR/Vision/RAG context through the existing agent flow

Memory is only saved when the user explicitly asks with phrases like `remember`, `save`, `store`, `from now on`, or `use this in future`.

## Security

- Requires JWT authorization.
- Blocks obvious sensitive data such as passwords, secrets, API keys, tokens, PINs, private keys, and connection strings.
- Does not auto-store ordinary chat content.
- Uses user-scoped SQL queries for all operations.

## Performance

- Uses `IDistributedCache` with the existing Redis or in-memory cache registration.
- Adds indexes for user/category lookups, pinned memory retrieval, and key search.
- Loads only a bounded number of active memories into the prompt context.

## Frontend

The `AI Memory` page supports:

- Saved Preferences
- Pinned Memories
- Project Memories
- Favorite Prompts/Items
- Recent Memories
- Category filtering
- Search
- Edit
- Delete
- Enable/Disable
- Pin/Favorite

## Deployment Steps

1. Deploy API and UI.
2. Run `Database/Scripts/030_LongTermMemory.sql`.
3. Restart API instances to clear old DI/app state.
4. Open `/memory` and create a test preference.
5. Ask chat a follow-up request that should use the saved preference.

## Testing Strategy

- Unit test `LongTermMemoryService` for explicit-memory detection, sensitive-data rejection, category/key inference, and context building.
- Integration test memory CRUD with two users to verify isolation.
- API test every `/api/memory` endpoint with JWT.
- Regression test existing `/api/chat`, Conversation Memory, RAG, OCR, Vision AI, and chat UI.
