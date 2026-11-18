using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kharasana.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KharasanaDbContext>
    {
        public KharasanaDbContext CreateDbContext(string[] args)
        {
            // Get environment CORE_ENVIRONMENT variable (default to "Development")
            var environmentName = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT") ?? "Development";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Kharasana.API"))
                // Load appsettings.json from the API project's directory
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<KharasanaDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string not found in appsettings.");
            }

            builder.UseSqlServer(connectionString);

            return new KharasanaDbContext(builder.Options);
        }
    }
}