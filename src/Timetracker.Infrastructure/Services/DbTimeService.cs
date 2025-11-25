using System.Text.Json;
using Timetracker.Core;
using Timetracker.Infrastructure.Caching;
using Timetracker.Infrastructure.Entities;

namespace Timetracker.Infrastructure
{
    public class DbTimeService : ITimeEntryService, IDisposable
    {
        private readonly ICachedDbSets _cachedDbSets;
        private readonly IClock _clock;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        // in-memory mirror of current running entry
        private TimeEntry? _current;
        private int _stopStartInProgress;

        public DbTimeService(IClock clock, ICachedDbSets cachedDbSets)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _cachedDbSets = cachedDbSets ?? throw new ArgumentNullException(nameof(cachedDbSets));

            _cachedDbSets.RefreshAllAsync().GetAwaiter().GetResult();
            InitializeCurrentFromDb();
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public TimeEntry? Current => _current;

        public event EventHandler<TimeEntry>? EntryStopped;

        public async Task StartAsync()
        {
            // If already running, noop.
            if (_current != null)
                return;

            var now = _clock.Now;
            var newEntry = new TimeEntry(Guid.NewGuid(), now, null);

            // Persist the started entry so crash won't lose it
            var entity = new TimeEntryEntity
            {
                Id = newEntry.Id,
                Start = newEntry.Start,
                End = newEntry.End,
                CreatedAt = now,
                ModifiedAt = null
            };

            await _cachedDbSets.For<TimeEntryEntity>().AddAsync(entity).ConfigureAwait(false);

            _current = MapToDomain(entity);
        }

        public Task ToggleAsync() => _current == null ? StartAsync() : StopAsync();

        public async Task StopAsync(StopOptions? options = null)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_current == null) return;

                var now = _clock.Now;
                var finishedId = _current.Id;
                var finished = _current with { End = now };

                // Use a transaction to update the TimeEntry and optionally persist EntryDescription
                await _cachedDbSets.ExecuteInTransactionAsync(async (db) =>
                {
                    // update the existing entry
                    var entity = await db.Set<TimeEntryEntity>().FindAsync(finishedId).ConfigureAwait(false);
                    if (entity == null)
                    {
                        // fallback: create a new entity if missing (shouldn't happen)
                        entity = new TimeEntryEntity
                        {
                            Id = finished.Id,
                            Start = finished.Start,
                            End = finished.End,
                            CreatedAt = finished.Start,
                            ModifiedAt = now
                        };
                        db.Add(entity);
                    }
                    else
                    {
                        entity.End = finished.End;
                        entity.ModifiedAt = now;
                        db.Update(entity);
                    }

                    // persist description if provided
                    if (options != null)
                    {
                        var desc = new EntryDescriptionEntity
                        {
                            TimeEntryId = finished.Id,
                            Description = options.Description,
                            JsonData = options.Fields == null || options.Fields.Count == 0
                                ? null
                                : JsonSerializer.Serialize(options.Fields)
                        };

                        // Upsert pattern: try find existing description
                        var existingDesc = await db.Set<EntryDescriptionEntity>().FindAsync(finished.Id)
                            .ConfigureAwait(false);
                        if (existingDesc == null)
                        {
                            db.Add(desc);
                        }
                        else
                        {
                            existingDesc.Description = desc.Description;
                            existingDesc.JsonData = desc.JsonData;
                            db.Update(existingDesc);
                        }
                    }

                    // no SaveChanges here; ExecuteInTransactionAsync will SaveChanges and commit
                }).ConfigureAwait(false);

                // After transaction commit, cached sets have been refreshed by ExecuteInTransactionAsync
                _current = null;
                EntryStopped?.Invoke(this, finished);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StopStartAsync(StopOptions? options = null)
        {
            // fast-guard so only one caller performs stop+start
            if (Interlocked.Exchange(ref _stopStartInProgress, 1) == 1)
                return;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var now = _clock.Now;

                if (_current != null)
                {
                    var finished = _current with { End = now };

                    // Transactionally update the stopped entry and add a new one
                    await _cachedDbSets.ExecuteInTransactionAsync(async (db) =>
                    {
                        // update stopped entry
                        var entity = await db.Set<TimeEntryEntity>().FindAsync(finished.Id).ConfigureAwait(false);
                        if (entity != null)
                        {
                            entity.End = finished.End;
                            entity.ModifiedAt = now;
                            db.Update(entity);
                        }
                        else
                        {
                            db.Add(new TimeEntryEntity
                            {
                                Id = finished.Id,
                                Start = finished.Start,
                                End = finished.End,
                                CreatedAt = finished.Start,
                                ModifiedAt = now
                            });
                        }

                        // persist description if provided
                        if (options != null)
                        {
                            var desc = new EntryDescriptionEntity
                            {
                                TimeEntryId = finished.Id,
                                Description = options.Description,
                                JsonData = options.Fields == null || options.Fields.Count == 0
                                    ? null
                                    : JsonSerializer.Serialize(options.Fields)
                            };

                            var existingDesc = await db.Set<EntryDescriptionEntity>().FindAsync(finished.Id)
                                .ConfigureAwait(false);
                            if (existingDesc == null)
                                db.Add(desc);
                            else
                            {
                                existingDesc.Description = desc.Description;
                                existingDesc.JsonData = desc.JsonData;
                                db.Update(existingDesc);
                            }
                        }

                        // start new entry
                        var newEntity = new TimeEntryEntity
                        {
                            Id = Guid.NewGuid(),
                            Start = now,
                            End = null,
                            CreatedAt = now,
                            ModifiedAt = null
                        };
                        db.Add(newEntity);

                        // no SaveChanges here; ExecuteInTransactionAsync will SaveChanges and commit
                    }).ConfigureAwait(false);

                    // caches refreshed by ExecuteInTransactionAsync; update in-memory current from cache
                    var setSnapshot = _cachedDbSets.For<TimeEntryEntity>().Entities;
                    var running = setSnapshot.FirstOrDefault(e => e.End == null);
                    _current = running == null ? null : MapToDomain(running);

                    EntryStopped?.Invoke(this, finished);
                }
                else
                {
                    // No current: just start a new entry
                    var newEntity = new TimeEntryEntity
                    {
                        Id = Guid.NewGuid(),
                        Start = now,
                        End = null,
                        CreatedAt = now,
                        ModifiedAt = null
                    };

                    await _cachedDbSets.For<TimeEntryEntity>().AddAsync(newEntity).ConfigureAwait(false);
                    _current = MapToDomain(newEntity);
                }
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Exchange(ref _stopStartInProgress, 0);
            }
        }

        public async Task<TimeEntry[]> GetRecentAsync(int max = 50)
        {
            var items = _cachedDbSets.For<TimeEntryEntity>().Entities
                .OrderByDescending(t => t.Start)
                .Take(max)
                .ToArray();

            // map to domain records
            return items.Select(e => new TimeEntry(e.Id, e.Start, e.End)).ToArray();
        }

        private void InitializeCurrentFromDb()
        {
            var running = _cachedDbSets.For<TimeEntryEntity>().Entities.FirstOrDefault(e => e.End == null);
            if (running != null)
            {
                _current = MapToDomain(running);
            }
        }

        private static TimeEntry MapToDomain(TimeEntryEntity e)
        {
            return new TimeEntry(e.Id, e.Start, e.End);
        }
    }
}