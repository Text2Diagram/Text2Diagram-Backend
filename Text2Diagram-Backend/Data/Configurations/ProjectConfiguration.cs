using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data.Models;
using System.Reflection.Emit;
using System.Text.Json;

namespace Text2Diagram_Backend.Data.Configurations
{
	public class ProjectConfiguration : IEntityTypeConfiguration<Project>
	{
		public void Configure(EntityTypeBuilder<Project> builder)
		{
			builder.HasKey(d => d.Id);
			builder.Property(d => d.Id)
				.ValueGeneratedNever();
			builder.Property(d => d.WorkspaceId)
				.IsRequired();

		}
	}
}
