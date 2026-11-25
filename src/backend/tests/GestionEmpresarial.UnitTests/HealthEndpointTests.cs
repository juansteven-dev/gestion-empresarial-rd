using System.Net;
namespace GestionEmpresarial.UnitTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class HealthEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
