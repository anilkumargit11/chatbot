namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class AdminUserModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

public sealed class AdminRoleModel
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public sealed class PermissionModel
{
    public int Id { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public bool IsActive { get; set; }
}
