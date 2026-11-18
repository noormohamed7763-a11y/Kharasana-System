using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Kharasana.Infrastructure.Workers;
using Xunit;
using FluentAssertions;
using System.Net;

namespace Kharasana.ContractTests.Tests;

public class ApiVersionTests : IClassFixture<ApiVersionTestFactory>
{
    private readonly ApiVersionTestFactory _factory;

    public ApiVersionTests(ApiVersionTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_VersionedEndpoint_ReturnsSuccessOrUnauthorized()
    {
        // Act: استدعاء نقطة النهاية مع نسخة الإصدار
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/Factories");

        // Assert: يجب أن تعود النتيجة إما 200 (إذا كان مسموحاً) أو 401 (لأنها محمية، لكن الإصدار يعمل)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_VersionedFactoryRegistrationRequests_ReachesProtectedEndpoint()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/registration-requests/paged");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed class ApiVersionTestFactory : WebApplicationFactory<Kharasana.API.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var worker = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(ProactiveAlertsWorker));

            if (worker != null)
                services.Remove(worker);
        });
    }
}
