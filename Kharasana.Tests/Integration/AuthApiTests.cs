using System.Net;
using System.Net.Http.Json;
using Kharasana.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kharasana.Tests.Integration
{
    public class AuthApiTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiFactory _factory;

        public AuthApiTests(ApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsCreated()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                FullName = "مختبر جديد",
                Phone = "770000000",
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/register", dto);

            // Assert
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Expected Created, but got {response.StatusCode}. Content: {content}");
            }
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
