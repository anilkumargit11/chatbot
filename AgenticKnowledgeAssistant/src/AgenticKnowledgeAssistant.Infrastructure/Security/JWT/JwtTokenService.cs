using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AgenticKnowledgeAssistant.Common.JWT;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public LoginResponseDTO GenerateToken(string userName, IEnumerable<string>? roles = null)
    {
        return GenerateToken(0, userName, roles);
    }

    public LoginResponseDTO GenerateToken(int userId, string email, IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new("uid", userId.ToString())
        };

        claims.AddRange((roles ?? Array.Empty<string>()).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange((permissions ?? Array.Empty<string>()).Select(permission => new Claim("permission", permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponseDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt,
            Roles = roles ?? Array.Empty<string>(),
            Permissions = permissions ?? Array.Empty<string>()
        };
    }
}
