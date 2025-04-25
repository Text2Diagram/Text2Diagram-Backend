using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public DbSet<Diagram> Diagrams { get; set; } = default!;
    public DbSet<Share> Shares { get; set; } = default!;
    public DbSet<Workspace> Workspaces { get; set; } = default!;
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; } = default!;
    public DbSet<Project> Projects { get; set; } = default!;
}
