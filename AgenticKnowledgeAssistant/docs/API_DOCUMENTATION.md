# API Documentation

All business APIs follow this pattern:

```text
Controller -> Application BAL/Service -> Infrastructure DAL/Repository -> SQL Server / AI Provider -> Response DTO
```

Most endpoints require JWT authentication unless explicitly marked as health/public.

## Response Wrapper

```json
{
  "ReturnCode": 200,
  "ReturnMessage": "success",
  "ResponseTime": "25",
  "Data": {}
}
```

## Authentication APIs

| API | Method | Purpose | Request DTO | Service/BAL | DAL/DB |
|---|---|---|---|---|---|
| `/api/auth/register`, `/api/RegisterUser` | POST | Register a user | `RegisterRequestDTO` | `AuthBAL.Register` | `AuthDAL`, user stored procedures |
| `/api/auth/login`, `/api/LoginUser`, `/api/auth/token` | POST | Login and issue tokens | `LoginRequestDTO` | `LoginCommand`, `AuthBAL.Login` | `AuthDAL` |
| `/api/auth/refresh`, `/api/auth/refresh-token`, `/api/RefreshToken` | POST | Refresh JWT | `RefreshTokenRequestDTO` | `AuthBAL.RefreshToken` | `AuthDAL` |
| `/api/auth/logout`, `/api/Logout` | POST | Revoke refresh token/session | `RefreshTokenRequestDTO` | `AuthBAL.Logout` | `AuthDAL` |

### Authentication Flow

```text
Request -> AuthController -> AuthBAL/CQRS -> AuthDAL -> SQL Server -> JwtTokenService -> Response
```

## MFA APIs

| API | Method | Purpose | Request DTO | Service |
|---|---|---|---|---|
| `/api/mfa/status` | GET | Read current MFA state | None | Infrastructure MFA service |
| `/api/mfa/setup-authenticator` | POST | Generate authenticator setup | None | MFA service |
| `/api/mfa/verify-setup` | POST | Verify setup code | `MfaCodeRequest` | MFA service |
| `/api/mfa/disable` | POST | Disable MFA | None | MFA service |
| `/api/mfa/verify-login` | POST | Verify MFA during login | `MfaLoginVerifyRequest` | MFA service |

## Chat APIs

| API | Method | Purpose | Request DTO | Service/BAL | DAL/Provider |
|---|---|---|---|---|---|
| `/api/chat` | POST | Smart Auto assistant chat | `ChatRequestDTO` | `ChatBAL`, `AgentBAL` | `ChatDAL`, `DocumentDAL`, `AgentDAL`, AI providers |
| `/api/chat/health` | GET | Chat health check | None | None | None |

### Chat Request Flow

```text
React ChatAssistantPage
  -> chatApi.send()
  -> ChatController.Chat
  -> ChatBAL.Chat
  -> AgentBAL.HandleAgentRequest
  -> Intent detection
  -> Document / database / OCR / general AI route
  -> Save chat history
  -> Response
```

## Document APIs

| API | Method | Purpose | Request | BAL | DAL/DB |
|---|---|---|---|---|---|
| `/api/document/upload` | POST | Upload and index document | `IFormFile file` | `DocumentBAL.UploadDocument` | `DocumentDAL.SaveDocumentDB`, `AgentDAL.SaveEmbeddingDB` |
| `/api/document` | GET | List documents | None | `DocumentBAL.GetDocuments` | `DocumentDAL.GetDocumentsDB` |
| `/api/document/search?q=value` | GET | Search documents | Query string | `DocumentBAL.SearchDocuments` | `DocumentDAL.SearchDocumentsDB` |
| `/api/document/{id}` | DELETE | Soft delete document | Route id | `DocumentBAL.DeleteDocument` | `DocumentDAL.DeleteDocumentDB` |
| `/api/document/health` | GET | Document health check | None | None | None |

## User Administration APIs

| API | Method | Purpose | Request DTO | BAL |
|---|---|---|---|---|
| `/api/users` | GET | Search/list users | Query filters | `UserAdminBAL.GetUsers` |
| `/api/users/{id}` | GET | Get user details | Route id | `UserAdminBAL.GetUser` |
| `/api/users` | POST | Create user | `SaveUserRequestDTO` | `UserAdminBAL.SaveUser` |
| `/api/users/{id}` | PUT | Update user | `SaveUserRequestDTO` | `UserAdminBAL.SaveUser` |
| `/api/users/{id}` | DELETE | Delete user | Route id | `UserAdminBAL.DeleteUser` |
| `/api/users/{id}/activate` | PUT | Activate user | Route id | `UserAdminBAL.ActivateUser` |
| `/api/users/{id}/deactivate` | PUT | Deactivate user | Route id | `UserAdminBAL.DeactivateUser` |
| `/api/users/{id}/reset-password` | PUT | Reset user password | `ResetPasswordRequestDTO` | `UserAdminBAL.ResetPassword` |

## Role and Permission APIs

| API | Method | Purpose | Request DTO | BAL |
|---|---|---|---|---|
| `/api/roles` | GET | List roles | Query filters | `RoleAdminBAL.GetRoles` |
| `/api/roles/{id}` | GET | Get role details | Route id | `RoleAdminBAL.GetRole` |
| `/api/roles` | POST | Create role | `SaveRoleRequestDTO` | `RoleAdminBAL.SaveRole` |
| `/api/roles/{id}` | PUT | Update role | `SaveRoleRequestDTO` | `RoleAdminBAL.SaveRole` |
| `/api/roles/{id}` | DELETE | Delete role | Route id | `RoleAdminBAL.DeleteRole` |
| `/api/roles/{id}/permissions` | GET | Get role permissions | Route id | `RoleAdminBAL.GetRolePermissions` |
| `/api/roles/{id}/permissions` | PUT | Assign permissions | `AssignPermissionsRequestDTO` | `RoleAdminBAL.AssignPermissions` |
| `/api/permissions` | GET | List permissions | None | `RoleAdminBAL.GetPermissions` |

## AI Provider Settings APIs

| API | Method | Purpose | Request | Service |
|---|---|---|---|---|
| `/api/ai-providers` | GET | Read masked provider settings and provider status | None | `IOptionsMonitor<AIProviderOptions>`, `IAIProviderResolver` |
| `/api/ai-providers` | POST | Save local provider settings | `AIProviderOptions` | writes `ai-provider-settings.local.json` |
| `/api/ai-providers/test` | POST | Test configured providers | None | `IAIProvider.GenerateChatCompletionAsync` |

Secrets are masked before returning settings to the UI.

## Tools APIs

| API | Method | Purpose |
|---|---|---|
| `/api/tools/date` | GET | Return server date/time |
| `/api/tools/search-files?query=value` | GET | Search local files/tool metadata |
| `/api/tools/search-database?query=value` | GET | Search database metadata |
| `/api/tools/health` | GET | Tools health check |

## Status APIs

| API | Method | Purpose |
|---|---|---|
| `/api/status` | GET | Application status |
| `/api/health` | GET | Health check |

## Validation and Security Notes

- Controllers use DTOs from `AgenticKnowledgeAssistant.Application/DTOs`.
- JWT auth is configured in `Program.cs`.
- `[Authorize]` protects primary controllers.
- Rate limiting is configured globally.
- File upload validation is handled in application helpers/BAL.
- SQL access should remain through DAL/stored procedures, not direct controller SQL.

## API Design Improvements Recommended

| Priority | Recommendation |
|---|---|
| High | Standardize all routes on resource-style paths while keeping legacy compatibility routes |
| High | Add FluentValidation validators for request DTOs |
| Medium | Add Swagger examples for each DTO |
| Medium | Add API versioning, for example `/api/v1/chat` |
| Medium | Add integration tests for auth, chat, document, users, and roles |
