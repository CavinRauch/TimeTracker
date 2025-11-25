using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Timetracker.Infrastructure.Entities;

[Table("EntryDescriptions")]
[Index(nameof(TimeEntryId), IsUnique = true)]
public class EntryDescriptionEntity
{
    [Key] [ForeignKey(nameof(TimeEntry))] public Guid TimeEntryId { get; set; }

    public string? Description { get; set; }

    public string? JsonData { get; set; }

    // Not mapped - convenience access
    [NotMapped]
    public IDictionary<string, string>? Metadata
    {
        get => string.IsNullOrEmpty(JsonData) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(JsonData);
        set => JsonData = (value == null || value.Count == 0) ? null : JsonSerializer.Serialize(value);
    }

    // Navigation properties
    public virtual TimeEntryEntity TimeEntry { get; set; } = null!;
}