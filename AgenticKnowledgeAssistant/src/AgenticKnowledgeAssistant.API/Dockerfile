FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln ./
COPY src/AgenticKnowledgeAssistant.API/*.csproj ./src/AgenticKnowledgeAssistant.API/
COPY src/AgenticKnowledgeAssistant.BAL/*.csproj ./src/AgenticKnowledgeAssistant.BAL/
COPY src/AgenticKnowledgeAssistant.DAL/*.csproj ./src/AgenticKnowledgeAssistant.DAL/
COPY src/AgenticKnowledgeAssistant.DTO/*.csproj ./src/AgenticKnowledgeAssistant.DTO/
COPY src/AgenticKnowledgeAssistant.Security/*.csproj ./src/AgenticKnowledgeAssistant.Security/
COPY src/AgenticKnowledgeAssistant.Common/*.csproj ./src/AgenticKnowledgeAssistant.Common/

# Restore dependencies
RUN dotnet restore

# Copy all source files and compile
COPY . .
WORKDIR /app/src/AgenticKnowledgeAssistant.API
RUN dotnet publish -c Release -o /app/publish

# Stage 2: ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from:build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "AgenticKnowledgeAssistant.API.dll"]
