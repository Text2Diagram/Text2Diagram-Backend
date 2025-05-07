using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Text2Diagram_Backend.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
        .IsRequired()
        .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.CreatedAt)
        .HasDefaultValueSql("NOW()");

        builder.Property(d => d.Data)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, JToken>>(v)!);

        builder.HasMany<Diagram>()
            .WithOne()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
