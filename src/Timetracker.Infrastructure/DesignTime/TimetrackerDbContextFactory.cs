using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Timetracker.Infrastructure.Context;

namespace Timetracker.Infrastructure.DesignTime
{
    public class TimetrackerDbContextFactory : IDesignTimeDbContextFactory<TimetrackerDbContext>
    {
        public TimetrackerDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TimetrackerDbContext>();

            // Use the same connection string you will use at runtime (or a dev-specific one)
            builder.UseSqlite(InfrastructureConstants.ConnectionString);

            return new TimetrackerDbContext(builder.Options);
        }
    }
}