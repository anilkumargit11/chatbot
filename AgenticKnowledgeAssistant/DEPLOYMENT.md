# Deployment

## Backend CI/CD

```bash
dotnet restore AgenticKnowledgeAssistant.sln
dotnet build AgenticKnowledgeAssistant.sln -c Release
dotnet publish src/AgenticKnowledgeAssistant.API/AgenticKnowledgeAssistant.API.csproj -c Release -o ./publish/api
```

## Frontend CI/CD

```bash
cd Frontend/agentic-knowledge-ui
npm ci
npm run build
```

## Required Production Settings

```text
ASPNETCORE_ENVIRONMENT=Production
AppSettings__DefaultConnection=<production-sql-connection>
AppSettings__OpenAIEndpoint=https://api.openai.com
AppSettings__OpenAIApiKey=<openai-key>
AppSettings__JWT_Secret=<32+ character signing key>
AppSettings__APIRateLimit=120
AppSettings__APIRateLimitSeconds=60
```

## Security Checklist

- Replace the sample JWT secret.
- Store SQL and OpenAI secrets outside source control.
- Restrict CORS origins in production.
- Keep HTTPS enforced.
- Add real user validation behind `/api/auth/token`.
- Use SQL Full-Text Search for large document search.
