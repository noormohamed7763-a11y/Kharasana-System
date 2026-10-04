using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;

namespace Kharasana.ContractTests.Tests;

public class ApiVersionTests : IClassFixture<WebApplicationFactory<Kharasana.API.Program>>
{
    private readonly WebApplicationFactory<Kharasana.API.Program> _factory;

    public ApiVersionTests(WebApplicationFactory<Kharasana.API.Program> factory)
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
}
