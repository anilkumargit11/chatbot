using System.Collections.Generic;

namespace AgenticKnowledgeAssistant.Domain.Entities;

public class User : AuditableEntity
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
}

public class Role : AuditableEntity
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; } = new List<User>();
}
