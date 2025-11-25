using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timetracker.Infrastructure.Entities;

public class AppSettingsEntity
{
    [Key] public int Id { get; set; } = 1;

    [Required] public bool PromptOnStop { get; set; } = true;

    public string? DefaultMetadataJson { get; set; }

    [Required] public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    [Timestamp] public byte[]? RowVersion { get; set; }
}

public class AppSettingsEntityConfiguration : IEntityTypeConfiguration<AppSettingsEntity>
{
    public void Configure(EntityTypeBuilder<AppSettingsEntity> builder)
    {
        // seed single row
        builder.HasData(new AppSettingsEntity
        {
            Id = 1,
            PromptOnStop = true,
            DefaultMetadataJson = null,
            ModifiedAt = DateTime.MinValue.AddYears(2000)
        });
    }
}