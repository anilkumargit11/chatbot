namespace AgenticKnowledgeAssistant.DTO.RequestDTOs;

public sealed class SaveUserRequestDTO
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ResetPasswordRequestDTO
{
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class SaveRoleRequestDTO
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class AssignPermissionsRequestDTO
{
    public IReadOnlyList<int> PermissionIds { get; set; } = Array.Empty<int>();
}
