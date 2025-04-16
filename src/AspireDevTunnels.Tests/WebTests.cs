namespace AspiresDevTunnels.Tests;

public class WebTests(ITestContextAccessor testContextAccessor)
{
    [Fact(Skip = "This test has dependency on Microsoft.devtunnels installation and login. Need to configure in CI/CD pipeline")]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        IDistributedApplicationTestingBuilder appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireDevTunnels_AppHost>(
                testContextAccessor.Current.CancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using Aspire.Hosting.DistributedApplication app =
            await appHost.BuildAsync(testContextAccessor.Current.CancellationToken);

        ResourceNotificationService resourceNotificationService =
            app.Services.GetRequiredService<ResourceNotificationService>();

        await app.StartAsync(testContextAccessor.Current.CancellationToken);

        // Act
        HttpClient httpClient = app.CreateHttpClient("webfrontend");

        await resourceNotificationService
            .WaitForResourceAsync("webfrontend", KnownResourceStates.Running, testContextAccessor.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(30), testContextAccessor.Current.CancellationToken);

        HttpResponseMessage response = await httpClient.GetAsync("/", testContextAccessor.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
