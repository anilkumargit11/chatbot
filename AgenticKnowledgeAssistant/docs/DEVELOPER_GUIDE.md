# Developer Guide

This guide teaches the project from beginner to enterprise level.

## 1. How to Read This Solution

Start from the request entry point and follow the dependency direction:

```text
Controller -> BAL/Service -> DAL/Repository -> SQL/Provider
```

Do not start from SQL first unless you are debugging a stored procedure. In a Clean Architecture solution, the controller tells you the use case, the BAL tells you the business rule, and the DAL tells you how data is fetched.

## 2. Important Folders

| Folder | Why it exists | When to change it |
|---|---|---|
| `src/AgenticKnowledgeAssistant.API/Controllers` | Public HTTP endpoints | Add/change API routes |
| `src/AgenticKnowledgeAssistant.API/Middleware` | Cross-cutting request pipeline | Add global headers, exception handling, logging |
| `src/AgenticKnowledgeAssistant.Application/Services` | Business logic and AI orchestration | Add feature behavior |
| `src/AgenticKnowledgeAssistant.Application/Interfaces` | Contracts for DAL/security/cache | Add dependency abstractions |
| `src/AgenticKnowledgeAssistant.Application/DTOs` | API/service request and response shapes | Change payloads carefully |
| `src/AgenticKnowledgeAssistant.Domain/Entities` | Core business entities | Add domain objects |
| `src/AgenticKnowledgeAssistant.Infrastructure/DataAccess` | SQL Server stored procedure access | Change database interaction |
| `Database` | SQL deployment assets | Add tables/SPs/indexes |
| `Frontend/agentic-knowledge-ui` | React UI | Change user experience |

## 3. Main Backend Classes

| Class | Purpose |
|---|---|
| `ChatController` | Receives chat requests from React |
| `ChatBAL` | Wraps chat request, calls agent, saves chat history |
| `AgentBAL` | Smart Auto router: documents, database, OCR, general AI, semantic BRD analysis |
| `DocumentBAL` | Validates/upload documents and coordinates indexing |
| `AuthBAL` | Login/register/refresh/logout business logic |
| `UserAdminBAL` | Admin user management |
| `RoleAdminBAL` | Roles and permissions management |
| `CommonDAL` | Shared ADO.NET execution helper |
| `ChatDAL` | Saves and reads chat history |
| `DocumentDAL` | Reads/searches/saves document data |
| `DatabaseAssistantDAL` | Database metadata assistant access |

## 4. Example: Chat Request

```text
User enters question
  -> ChatAssistantPage.tsx
  -> chatApi.ts
  -> POST /api/chat
  -> ChatController
  -> ChatBAL
  -> AgentBAL
  -> AI provider or document/database path
  -> ChatDAL saves history
  -> Response returned to UI
```

Simple Telugu explanation:

```text
User question UI lo type chestaru.
API ki request velthundi.
Controller request receive chestundi.
BAL business logic decide chestundi.
DAL database tho matladutundi.
Final answer malli UI ki return avutundi.
```

## 5. Example: Document Upload

```text
Upload file
  -> DocumentController.UploadDocument
  -> DocumentBAL.UploadDocument
  -> Validate file
  -> Extract readable content
  -> DocumentDAL.SaveDocumentDB
  -> AgentDAL.SaveEmbeddingDB where available
  -> Return upload summary
```

## 6. Configuration Rules

- Use `appsettings.json` for defaults.
- Use `appsettings.Development.json` for development-safe overrides.
- Use `ai-provider-settings.local.json` or environment variables for local provider secrets.
- Do not commit real API keys.
- Production should use environment variables, Azure Key Vault, or another secrets manager.

## 7. Adding a New Feature

1. Add request/response DTOs in `Application/DTOs`.
2. Add interface contract if the feature needs database/external access.
3. Implement business logic in `Application/Services`.
4. Implement data access in `Infrastructure/DataAccess`.
5. Register services in `API/Extensions/ServiceCollectionExtensions.cs`.
6. Add controller endpoint in `API/Controllers`.
7. Add SQL script under the correct `Database` subfolder.
8. Add/update React service and page/component.
9. Build backend and frontend.

## 8. Interview Questions You Can Expect

| Question | Short Answer |
|---|---|
| Why use Clean Architecture? | To keep business logic independent from web/database/framework details. |
| Why use interfaces for DAL? | To decouple Application from Infrastructure and make testing easier. |
| Why should controllers stay thin? | Controllers should handle HTTP, not business rules. |
| Why use DTOs? | DTOs keep API payloads separate from database/domain objects. |
| Why use stored procedures here? | They centralize SQL logic and allow controlled DB permissions. |
| Why use DI? | DI makes dependencies explicit, testable, and replaceable. |

## 9. Common Mistakes

- Putting SQL queries directly in controllers.
- Returning database entities directly to React.
- Hardcoding API keys or connection strings.
- Adding business rules in React instead of backend services.
- Swallowing exceptions without logging.
- Changing DTO shape without checking frontend consumers.
- Moving folders/projects without updating `.sln`, project references, Dockerfiles, and deployment scripts.

## 10. Build Checklist

```powershell
dotnet restore .\AgenticKnowledgeAssistant.sln
dotnet build .\AgenticKnowledgeAssistant.sln --no-restore
cd .\Frontend\agentic-knowledge-ui
npm run build
```

## 11. Production Readiness Checklist

- JWT secret comes from secure configuration.
- AI provider keys are not committed.
- SQL user has least privilege.
- Rate limiting is enabled.
- Logs are retained and monitored.
- Health endpoints are monitored.
- File upload size/type restrictions are enforced.
- Integration tests cover core APIs.
- Database scripts are versioned and reviewed.
