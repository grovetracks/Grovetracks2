using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Repositories;
using Grovetracks.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrovetracksDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<ISimpleCompositionMapper, SimpleCompositionMapper>();
        services.AddScoped<IQuickdrawSimpleDoodleRepository, QuickdrawSimpleDoodleRepository>();
        services.AddScoped<IDoodleEngagementRepository, DoodleEngagementRepository>();
        services.AddScoped<ISeedCompositionRepository, SeedCompositionRepository>();
        return services;
    }
}
