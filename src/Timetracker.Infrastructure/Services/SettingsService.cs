using Microsoft.EntityFrameworkCore;
using Timetracker.Infrastructure.Caching;
using Timetracker.Infrastructure.Context;
using Timetracker.Infrastructure.Entities;

namespace Timetracker.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ICachedDbSets _cached;
    private readonly IDbContextFactory<TimetrackerDbContext> _dbFactory;

    public SettingsService(IDbContextFactory<TimetrackerDbContext> dbFactory, ICachedDbSets cached)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _cached = cached ?? throw new ArgumentNullException(nameof(cached));
    }

    public async Task<AppSettingsEntity> GetAsync()
    {
        var cached = _cached.For<AppSettingsEntity>().Entities.FirstOrDefault();
        if (cached != null) return cached;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await db.Set<AppSettingsEntity>().FindAsync(1) ?? new AppSettingsEntity { Id = 1 };
        return settings;
    }

    public async Task UpdateAsync(AppSettingsEntity settings)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Set<AppSettingsEntity>().FindAsync(settings.Id);
        if (existing == null)
        {
            settings.ModifiedAt = DateTimeOffset.UtcNow;
            db.Add(settings);
        }
        else
        {
            existing.PromptOnStop = settings.PromptOnStop;
            existing.DefaultMetadataJson = settings.DefaultMetadataJson;
            existing.ModifiedAt = DateTimeOffset.UtcNow;
            db.Update(existing);
        }

        try
        {
            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        await _cached.For<AppSettingsEntity>().RefreshAsync().ConfigureAwait(false);
    }
}