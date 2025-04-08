using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data.Configurations;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.HasKey(wm => wm.Id);
        builder.Property(wm => wm.Id)
            .ValueGeneratedNever();
        builder.Property(wm => wm.UserId)
            .IsRequired();
        builder.Property(wm => wm.WorkspaceId)
            .IsRequired();
        builder.Property(wm => wm.Role)
            .IsRequired()
            .HasConversion(new ValueConverter<WorkspaceRole, string>(
                v => v.ToString(),
                v => (WorkspaceRole)Enum.Parse(typeof(WorkspaceRole), v)
            ));
    }
}