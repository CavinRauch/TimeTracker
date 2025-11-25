using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timetracker.Infrastructure.Entities;

[Table("TimeEntries")]
public class TimeEntryEntity
{
    [Key] public Guid Id { get; set; }

    [Required] public DateTimeOffset Start { get; set; }

    public DateTimeOffset? End { get; set; }

    [Required] public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // Navigation properties
    public virtual EntryDescriptionEntity? Description { get; set; }
}

internal class TimeEntryEntityConfiguration : IEntityTypeConfiguration<TimeEntryEntity>
{
    public void Configure(EntityTypeBuilder<TimeEntryEntity> builder)
    {
        // Ensure cascade delete for 1:1 relationship explicitly (annotations don't express cascade)
        builder.HasOne(te => te.Description)
            .WithOne(d => d.TimeEntry)
            .HasForeignKey<EntryDescriptionEntity>(d => d.TimeEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}