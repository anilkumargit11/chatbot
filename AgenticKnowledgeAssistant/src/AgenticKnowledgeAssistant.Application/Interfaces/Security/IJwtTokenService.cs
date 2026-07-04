using AgenticKnowledgeAssistant.DTO.ResponseDTOs;

namespace AgenticKnowledgeAssistant.Common.JWT;

public interface IJwtTokenService
{
    LoginResponseDTO GenerateToken(string userName, IEnumerable<string>? roles = null);
    LoginResponseDTO GenerateToken(int userId, string email, IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null);
}
