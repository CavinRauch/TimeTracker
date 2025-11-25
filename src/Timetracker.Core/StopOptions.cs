// Timetracker.Core/StopOptions.cs

namespace Timetracker.Core
{
    public record StopOptions(string? Description = null, IDictionary<string, string>? Fields = null);
}