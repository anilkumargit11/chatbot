using AgenticKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgenticKnowledgeAssistant.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserMfaSettings> MfaSettings => Set<UserMfaSettings>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map tables
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("tblAI_Users");
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("tblAI_Roles");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserMfaSettings>(entity =>
        {
            entity.ToTable("tblAI_UserMfaSettings");
            entity.HasKey(e => e.UserId);
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.ToTable("tblAI_Folders");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("tblAI_ChatSessions");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("tblAI_ChatHistory"); // map to the history table
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("tblAI_AuditLogs");
            entity.HasKey(e => e.Id);
        });
    }
}
