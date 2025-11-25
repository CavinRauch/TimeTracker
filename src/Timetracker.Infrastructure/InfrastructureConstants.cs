namespace Timetracker.Infrastructure;

public class InfrastructureConstants
{
    public static string ConnectionString => $"Data Source={Path.Combine(AppContext.BaseDirectory, "timetracker.db")}";
}