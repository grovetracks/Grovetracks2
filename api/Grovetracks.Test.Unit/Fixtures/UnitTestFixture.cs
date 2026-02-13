using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.Test.Unit.Fixtures;

public class UnitTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public UnitTestFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISimpleCompositionMapper, SimpleCompositionMapper>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
