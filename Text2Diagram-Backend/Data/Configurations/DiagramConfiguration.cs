using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data.Configurations;

public class DiagramConfiguration : IEntityTypeConfiguration<Diagram>
{
    public void Configure(EntityTypeBuilder<Diagram> builder)
    {
        builder.HasKey(d => d.Id);
		builder.Property(d => d.Id)
			.IsRequired()
			.HasDefaultValueSql("gen_random_uuid()");
		builder.Property(d => d.Title).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(1000);


        builder.Property(d => d.DiagramData)
            .IsRequired();

        builder.Property(d => d.UserId).IsRequired();

        builder.Property(d => d.DiagramType)
            .IsRequired()
            .HasConversion(new ValueConverter<DiagramType, string>(
                v => v.ToString(),
                v => (DiagramType)Enum.Parse(typeof(DiagramType), v)
            ));

        builder.Property(d => d.DiagramJson)
            .IsRequired()
            .HasColumnType("jsonb");


        builder.HasMany(d => d.Shares)
            .WithOne()
            .HasForeignKey(s => s.DiagramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("NOW()");

		builder.Property(d => d.UpdatedAt)
			.HasDefaultValueSql("NOW()");
	}
}
