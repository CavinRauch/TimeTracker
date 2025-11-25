using Timetracker.Infrastructure.Entities;

namespace Timetracker.Infrastructure.Services;

public interface ISettingsService
{
    Task<AppSettingsEntity> GetAsync();
    Task UpdateAsync(AppSettingsEntity settings);
}