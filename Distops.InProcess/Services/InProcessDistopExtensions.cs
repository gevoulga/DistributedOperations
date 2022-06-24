using Distops.Core.Extensions;
using Distops.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Distops.InProcess.Services;

public static class DistopExtensions
{
    public static IServiceCollection AddInProcessDistops(this IServiceCollection services)
    {
        services
            .AddDistopExecutor()
            .AddSingleton<InProcessDistopClient>();
        services.TryAddSingleton<IDistopClient>(sp => sp.GetRequiredService<InProcessDistopClient>());
        return services;
    }
}