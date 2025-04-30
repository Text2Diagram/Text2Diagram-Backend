using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data.Configurations;

public class ShareConfiguration : IEntityTypeConfiguration<Share>
{
    public void Configure(EntityTypeBuilder<Share> builder)
    {
        builder.HasKey(s => s.Id);
		builder.Property(d => d.Id)
			.IsRequired()
			.HasDefaultValueSql("gen_random_uuid()");
		builder.Property(s => s.DiagramId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();

        builder.Property(s => s.Permission)
            .IsRequired()
            .HasConversion(new ValueConverter<SharePermission, string>(
                v => v.ToString(),
                v => (SharePermission)Enum.Parse(typeof(SharePermission), v)
            ));

        builder.HasOne<Diagram>()
            .WithMany(d => d.Shares)
            .HasForeignKey(s => s.DiagramId)
            .OnDelete(DeleteBehavior.Cascade);
	}
}
