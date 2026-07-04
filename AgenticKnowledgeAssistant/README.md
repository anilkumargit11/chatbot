# Agentic Knowledge Assistant

Enterprise AI Knowledge Assistant built with .NET 8, SQL Server, and React/Vite.

The repository is organized as a Clean Architecture solution with API, Application, Domain, Infrastructure, Database, Frontend, and Documentation areas. This matches a professional Visual Studio solution style while preserving the existing business logic.

## Solution Structure

```text
AgenticKnowledgeAssistant/
|-- AgenticKnowledgeAssistant.sln
|-- src/
|   |-- AgenticKnowledgeAssistant.API
|   |-- AgenticKnowledgeAssistant.Application
|   |-- AgenticKnowledgeAssistant.Domain
|   `-- AgenticKnowledgeAssistant.Infrastructure
|-- Database/
|   |-- Tables
|   |-- StoredProcedures
|   |-- Views
|   |-- Functions
|   |-- Constraints
|   |-- Indexes
|   `-- Scripts
|-- Frontend/
|   `-- agentic-knowledge-ui
|-- docs/
|   |-- ENTERPRISE_ARCHITECTURE.md
|   |-- API_DOCUMENTATION.md
|   `-- DEVELOPER_GUIDE.md
|-- scripts/
`-- Logs/
```

## Backend Projects

| Project | Responsibility |
|---|---|
| `AgenticKnowledgeAssistant.API` | HTTP controllers, middleware, filters, SignalR hubs, startup configuration, DI registration |
| `AgenticKnowledgeAssistant.Application` | BAL/services, interfaces, DTOs, CQRS handlers, validation/performance/logging behaviors, AI provider contracts |
| `AgenticKnowledgeAssistant.Domain` | Core entities and repository abstractions |
| `AgenticKnowledgeAssistant.Infrastructure` | SQL Server DAL, EF Core DbContext, repositories, cache, JWT/MFA/security implementations |

## Request Flow

```text
React UI
  -> API Controller
  -> Application BAL / Service
  -> Infrastructure DAL / Repository
  -> SQL Server stored procedure or table
  -> Response DTO / Response<object>
  -> React UI
```

## Run Backend

```powershell
dotnet restore .\AgenticKnowledgeAssistant.sln
dotnet build .\AgenticKnowledgeAssistant.sln
dotnet run --project .\src\AgenticKnowledgeAssistant.API\AgenticKnowledgeAssistant.API.csproj --launch-profile AgenticKnowledgeAssistant.API
```

Swagger:

```text
http://localhost:5242/swagger
https://localhost:5243/swagger
```

## Run Frontend

```powershell
cd .\Frontend\agentic-knowledge-ui
npm install
npm run dev
```

UI:

```text
http://localhost:5173
```

## Key Documentation

- [Enterprise Architecture](docs/ENTERPRISE_ARCHITECTURE.md)
- [API Documentation](docs/API_DOCUMENTATION.md)
- [Developer Guide](docs/DEVELOPER_GUIDE.md)
- [Database Guide](Database/README.md)
- [Deployment Notes](DEPLOYMENT.md)

## Current Enterprise Capabilities

- JWT authentication and refresh tokens
- MFA support
- RBAC user/role administration
- Chat assistant with Smart Auto routing
- Document upload and document search
- Semantic BRD section understanding
- AI provider abstraction for Azure OpenAI, OpenAI, Ollama, LM Studio, Local Llama/OpenAI-compatible endpoints, and Azure AI Foundry
- SQL/database assistant paths
- OCR/image context services
- React enterprise chat UI
- SQL Server schema scripts, stored procedures, indexes, and deployment scripts
