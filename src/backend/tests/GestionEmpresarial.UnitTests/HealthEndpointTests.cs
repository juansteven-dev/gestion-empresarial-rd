using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace GestionEmpresarial.UnitTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        const string aspnetEnvironment = "ASPNETCORE_ENVIRONMENT";
        const string dotnetEnvironment = "DOTNET_ENVIRONMENT";
        const string connectionSetting = "ConnectionStrings__DefaultConnection";
        var originalAspnetEnvironment = Environment.GetEnvironmentVariable(aspnetEnvironment);
        var originalDotnetEnvironment = Environment.GetEnvironmentVariable(dotnetEnvironment);
        var originalConnection = Environment.GetEnvironmentVariable(connectionSetting);
        Environment.SetEnvironmentVariable(aspnetEnvironment, "Testing");
        Environment.SetEnvironmentVariable(dotnetEnvironment, "Testing");
        Environment.SetEnvironmentVariable(
            connectionSetting,
            "Server=localhost;Database=HealthTests;Trusted_Connection=True;TrustServerCertificate=True");

        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                    builder.ConfigureLogging(logging => logging.ClearProviders()));
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            Environment.SetEnvironmentVariable(aspnetEnvironment, originalAspnetEnvironment);
            Environment.SetEnvironmentVariable(dotnetEnvironment, originalDotnetEnvironment);
            Environment.SetEnvironmentVariable(connectionSetting, originalConnection);
        }
    }
}
