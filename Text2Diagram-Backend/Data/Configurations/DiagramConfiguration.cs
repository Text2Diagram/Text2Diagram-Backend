﻿using Microsoft.EntityFrameworkCore;
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
            .ValueGeneratedNever();
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


        builder.HasMany(d => d.Shares)
            .WithOne()
            .HasForeignKey(s => s.DiagramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
