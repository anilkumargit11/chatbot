# Setup Guide

## Backend

```bash
dotnet restore AgenticKnowledgeAssistant.sln
dotnet build AgenticKnowledgeAssistant.sln
dotnet run --project src/AgenticKnowledgeAssistant.API/AgenticKnowledgeAssistant.API.csproj
```

Swagger:

```text
https://localhost:5243/swagger
```

## Database

Run scripts in this order:

```text
Database/Scripts/000_CreateDatabase.sql
Database/Tables/001_CreateTables.sql
Database/Indexes/001_IndexRecommendations.sql
Database/StoredProcedures/001_AgenticKnowledgeAssistantStoredProcedures.sql
Database/Scripts/999_OptimizationRecommendations.sql
```

## API Configuration

Use `src/AgenticKnowledgeAssistant.API/appsettings.json` or environment variables:

```json
{
  "AppSettings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Ajay_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "OpenAIEndpoint": "https://api.openai.com",
    "OpenAIApiKey": "YOUR_OPENAI_API_KEY",
    "JWT_Secret": "CHANGE_THIS_TO_A_32_CHARACTER_MINIMUM_SECRET",
    "APIRateLimit": 120,
    "APIRateLimitSeconds": 60
  }
}
```

## Frontend

```bash
cd Frontend/agentic-knowledge-ui
npm install
npm run dev
```

Frontend:

```text
http://localhost:5173
```
