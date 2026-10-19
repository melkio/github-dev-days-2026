namespace TaskFlow.IntegrationTests.Infrastructure;

public abstract class ApiTestBase : IAsyncLifetime
{
    private ApiFactory _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory();
        Client = _factory.CreateClient();
        await _factory.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
