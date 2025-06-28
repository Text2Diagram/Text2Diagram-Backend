using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data.Configurations;

public class TempDiagramConfiguration : IEntityTypeConfiguration<TempDiagram>
{
    public void Configure(EntityTypeBuilder<TempDiagram> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .IsRequired()
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.DiagramData)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(d => d.DiagramType)
            .IsRequired()
            .HasConversion(new ValueConverter<DiagramType, string>(
                v => v.ToString(),
                v => (DiagramType)Enum.Parse(typeof(DiagramType), v)
            ));

    }
}
