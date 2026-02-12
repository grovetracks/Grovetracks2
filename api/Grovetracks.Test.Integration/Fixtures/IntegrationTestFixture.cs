using Grovetracks.DataAccess;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Repositories;
using Grovetracks.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.Test.Integration.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgresFixture Postgres { get; } = new();
    public LocalStackFixture LocalStack { get; } = new();
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            Postgres.InitializeAsync(),
            LocalStack.InitializeAsync());

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(Postgres.ConnectionString));

        services.AddSingleton(LocalStack.S3Client);
        services.AddSingleton<IDrawingStorageService, S3DrawingStorageService>();
        services.AddSingleton<ICompositionMapper, QuickdrawCompositionMapper>();
        services.AddScoped<IQuickdrawDoodleRepository, QuickdrawDoodleRepository>();
        services.AddSingleton<ISimpleCompositionMapper, SimpleCompositionMapper>();
        services.AddScoped<IQuickdrawSimpleDoodleRepository, QuickdrawSimpleDoodleRepository>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        await Task.WhenAll(
            Postgres.DisposeAsync(),
            LocalStack.DisposeAsync());
    }
}
