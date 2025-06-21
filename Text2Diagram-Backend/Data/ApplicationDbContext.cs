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
    public DbSet<Project> Projects { get; set; } = default!;
}
