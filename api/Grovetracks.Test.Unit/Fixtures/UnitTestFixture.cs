using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Grovetracks.Test.Unit.Fixtures;

public class UnitTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public IDrawingStorageService MockDrawingStorageService { get; }

    public UnitTestFixture()
    {
        MockDrawingStorageService = Substitute.For<IDrawingStorageService>();

        var services = new ServiceCollection();
        services.AddSingleton(MockDrawingStorageService);
        services.AddSingleton<ICompositionMapper, QuickdrawCompositionMapper>();
        services.AddSingleton<ISimpleCompositionMapper, SimpleCompositionMapper>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
