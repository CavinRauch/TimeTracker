using Microsoft.EntityFrameworkCore;
using Timetracker.Infrastructure.Entities;

namespace Timetracker.Infrastructure.Context
{
    public class TimetrackerDbContext : DbContext
    {
        public TimetrackerDbContext(DbContextOptions<TimetrackerDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimeEntryEntity).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}