using Kharasana.API;
using Kharasana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kharasana.Tests.Integration
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // إزالة كل الخدمات المتعلقة بـ KharasanaDbContext
                var descriptors = services.Where(
                    d => d.ServiceType.ToString().Contains("KharasanaDbContext") ||
                         d.ServiceType == typeof(DbContextOptions<KharasanaDbContext>)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // إضافة DbContext جديد باستخدام In-Memory database
                services.AddDbContext<KharasanaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<KharasanaDbContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<ApiFactory>>();

                    db.Database.EnsureCreated(); // التأكد من إنشاء قاعدة البيانات

                    try
                    {
                        // تهيئة البيانات الأولية إذا لزم الأمر
                        // Utilities.InitializeDbForTests(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
                    }
                }
            });
        }
    }
}
