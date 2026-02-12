using Amazon.S3;
using Grovetracks.DataAccess.Interfaces;
using Grovetracks.DataAccess.Repositories;
using Grovetracks.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grovetracks.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrovetracksDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3, AmazonS3Client>();
        services.AddSingleton<IDrawingStorageService, S3DrawingStorageService>();
        services.AddSingleton<ICompositionMapper, QuickdrawCompositionMapper>();
        services.AddScoped<IQuickdrawDoodleRepository, QuickdrawDoodleRepository>();
        services.AddSingleton<ISimpleCompositionMapper, SimpleCompositionMapper>();
        services.AddScoped<IQuickdrawSimpleDoodleRepository, QuickdrawSimpleDoodleRepository>();
        services.AddScoped<IDoodleEngagementRepository, DoodleEngagementRepository>();
        services.AddScoped<ISeedCompositionRepository, SeedCompositionRepository>();
        return services;
    }
}
