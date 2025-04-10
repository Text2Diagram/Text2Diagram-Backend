﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Data.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .ValueGeneratedNever();
        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.HasMany(w => w.Diagrams)
            .WithOne()
            .HasForeignKey("WorkspaceId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
