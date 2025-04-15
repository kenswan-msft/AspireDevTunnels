namespace SampleShop.Tests;

public class WebTests(ITestContextAccessor testContextAccessor)
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.SampleShop_AppHost>(testContextAccessor.Current.CancellationToken);
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

        await using var app = await appHost.BuildAsync(testContextAccessor.Current.CancellationToken);
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync(testContextAccessor.Current.CancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");

        await resourceNotificationService
            .WaitForResourceAsync("webfrontend", KnownResourceStates.Running, testContextAccessor.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), testContextAccessor.Current.CancellationToken);

        var response = await httpClient.GetAsync("/", testContextAccessor.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
