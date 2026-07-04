# Enterprise Architecture

This document describes the current buildable architecture of Agentic Knowledge Assistant. It intentionally reflects the real projects in the solution, not old logical project names.

## 1. Architecture Overview

```text
Browser / React UI
    |
    v
AgenticKnowledgeAssistant.API
    Controllers, Middleware, Filters, Hubs, Program.cs
    |
    v
AgenticKnowledgeAssistant.Application
    BAL services, DTOs, interfaces, CQRS, AI provider contracts
    |
    v
AgenticKnowledgeAssistant.Infrastructure
    DAL, SQL access, repositories, EF Core DbContext, security implementations
    |
    v
SQL Server / External AI Providers
```

## 2. Project Dependency Direction

```text
API
 |-- Application
 |-- Infrastructure

Infrastructure
 |-- Application
 |-- Domain

Application
 |-- Domain

Domain
 `-- no project dependency on application/infrastructure/API
```

The dependency direction keeps business rules away from ASP.NET Core and SQL implementation details.

## 3. Folder Structure

```text
src/
|-- AgenticKnowledgeAssistant.API/
|   |-- Controllers
|   |-- Extensions
|   |-- Filters
|   |-- Hubs
|   |-- Middleware
|   |-- Services
|   |-- Program.cs
|   `-- appsettings.json
|-- AgenticKnowledgeAssistant.Application/
|   |-- Behaviors
|   |-- Common
|   |-- CQRS
|   |-- DTOs
|   |-- Interfaces
|   `-- Services
|-- AgenticKnowledgeAssistant.Domain/
|   |-- Entities
|   `-- Repositories
`-- AgenticKnowledgeAssistant.Infrastructure/
    |-- Cache
    |-- DataAccess
    |-- Persistence
    `-- Security
```

## 4. Layer Responsibilities

| Layer | Purpose | Examples |
|---|---|---|
| API | Receives HTTP requests, validates auth, calls BAL, returns HTTP response | `ChatController`, `DocumentController`, `AuthController` |
| Application | Business orchestration, DTOs, AI routing, document understanding, service contracts | `ChatBAL`, `AgentBAL`, `DocumentBAL`, `IChatDAL` |
| Domain | Core entities and stable abstractions | `User`, `ChatSession`, `IRepository` |
| Infrastructure | Database, cache, repositories, security providers | `ChatDAL`, `DocumentDAL`, `ApplicationDbContext`, `JwtTokenService` |
| Database | SQL Server deployment assets | Tables, stored procedures, indexes |
| Frontend | React/Vite application | Pages, services, contexts, components |

## 5. Request Flow

```text
1. User submits request in React.
2. React service calls an API endpoint through Axios.
3. ASP.NET Core authenticates JWT and runs middleware.
4. Controller logs the request and calls the BAL interface.
5. BAL validates intent and applies business rules.
6. BAL calls DAL/repository or AI provider as required.
7. DAL executes SQL Server stored procedures.
8. BAL wraps the result in `Response<object>` or a response DTO.
9. Controller maps return code to HTTP status.
10. React renders the answer, sources, confidence, and timing.
```

## 6. Chat / AI Flow

```text
ChatAssistantPage
  -> chatApi.send()
  -> POST /api/chat
  -> ChatController.Chat
  -> ChatBAL.Chat
  -> AgentBAL.HandleAgentRequest
      -> Detect intent
      -> Use image context / document search / database assistant / general AI
      -> AIProviderResolver selects configured provider
      -> Provider returns answer
  -> ChatDAL.SaveChatHistoryDB
  -> UI renders ChatMessageItem
```

## 7. Authentication Flow

```text
Login request
  -> AuthController.Login
  -> MediatR LoginCommand / AuthBAL
  -> AuthDAL validates user from SQL Server
  -> JwtTokenService creates access/refresh tokens
  -> Client stores token
  -> Subsequent requests include Authorization: Bearer <token>
```

MFA is handled by `MfaController` and the infrastructure MFA service.

## 8. Authorization Flow

```text
Request
  -> ASP.NET Core JWT middleware
  -> [Authorize]
  -> custom JWT filters where applied
  -> Controller action
```

Admin modules use BAL methods for role and permission operations.

## 9. Database Flow

```text
BAL service
  -> DAL interface
  -> Infrastructure DAL
  -> CommonDAL.QueryAsync / ExecuteAsync
  -> Stored procedure
  -> SQL Server result
```

Database scripts are separated under `Database/` by object type.

## 10. Logging and Exception Flow

```text
Request
  -> Serilog request/application logging
  -> BufferedCodeLogger controller step logging
  -> GlobalExceptionMiddleware catches unhandled exceptions
  -> Response wrapper / error response
```

Runtime file logs are written under API `Logs/`.

## 11. Configuration Flow

```text
appsettings.json
  -> appsettings.Development.json
  -> ai-provider-settings.local.json
  -> environment variables
  -> Program.cs options binding
  -> DI services consume options
```

Local secrets such as AI provider keys should stay in ignored local settings or environment variables.

## 12. Professional Solution Explorer View

```text
Solution 'AgenticKnowledgeAssistant'
|-- src
|   |-- AgenticKnowledgeAssistant.API
|   |-- AgenticKnowledgeAssistant.Application
|   |-- AgenticKnowledgeAssistant.Domain
|   `-- AgenticKnowledgeAssistant.Infrastructure
|-- Database
|   |-- Tables
|   |-- StoredProcedures
|   |-- Views
|   |-- Functions
|   |-- Constraints
|   |-- Indexes
|   `-- Scripts
|-- Frontend
|   `-- agentic-knowledge-ui
|-- docs
|-- scripts
`-- Logs
```

## 13. Improvement Roadmap

| Priority | Improvement | Reason |
|---|---|---|
| High | Add automated backend unit tests for BAL/DAL contracts | Protect chat/document/auth behavior |
| High | Add integration tests for `/api/chat`, `/api/document`, `/api/auth` | Catch routing/provider regressions |
| High | Move local secrets fully to environment/user secrets in production | Reduce secret exposure risk |
| Medium | Split large `AgentBAL` into Router, DocumentAnalyzer, DatabaseAssistant, GeneralAI services | Improve maintainability |
| Medium | Add typed options validation on startup | Fail fast for invalid provider/JWT/database config |
| Medium | Add pagination to admin/document/history endpoints | Improve performance at scale |
| Low | Add solution folders for docs/database/frontend in Visual Studio | Better IDE discoverability |
