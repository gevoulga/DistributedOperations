using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Distops.Core.Extensions;
using Distops.Core.Services;
using Distops.ServiceBus.Options;
using Distops.ServiceBus.Services;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Distops.ServiceBus.Extensions;

public static class ServiceBusDistopExtensions
{
    // services.Configure<CarvedRockApiOptions>(_config.GetSection(CarvedRockApiOptions.Section));
    public static IServiceCollection AddServiceBusDistopClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddServiceBus(services, configuration);
        services.TryAddSingleton<IDistopClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ServiceBusDistopClient>>();
            var options = sp.GetRequiredService<IOptions<ServiceBusDistopOptions>>();
            var client = sp.GetRequiredService<ServiceBusClient>();
            return new ServiceBusDistopClient(logger, options, client);
        });
        return services;
    }

    public static IServiceCollection AddServiceBusDistopExecutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddServiceBus(services, configuration);
        services
            .AddDistopExecutor()
            .AddSingleton<ServiceBusDistopExecutor>();
        return services;
    }

    private static void AddServiceBus(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ServiceBusDistopOptions.Section);
        services.Configure<ServiceBusDistopOptions>(section);

        // Add the service bus client
        services.AddAzureClients(builder =>
        {
            var serviceBusOptions = section.Get<ServiceBusDistopOptions>();

            // Check if fully qualified namespace is provided, or connection string
            var isNotFullyQualifiedNamespace =
                Uri.CheckHostName(serviceBusOptions.ServiceBusEndpoint) == UriHostNameType.Unknown;
            if (isNotFullyQualifiedNamespace)
            {
                builder.AddServiceBusClient(serviceBusOptions.ServiceBusEndpoint);
            }
            else
            {
                builder
                    .AddServiceBusClientWithNamespace(serviceBusOptions.ServiceBusEndpoint)
                    .WithCredential(new DefaultAzureCredential());
                // .WithCredential(sp =>
                // {
                //     var aadAuthOptions = sp.GetRequiredService<IOptions<AadAuthOptions>>().Value;
                //     var certManager = sp.GetRequiredService<ICertificateAccessor>();
                //     var certOptions = new ClientCertificateCredentialOptions { SendCertificateChain = true };
                //     var certificate = certManager.FindCertificateBySubjectName(
                //         aadAuthOptions.AADCertSubjectName,
                //         CommonConstants.CertUseCaseServiceBusAAD);
                //     return new ClientCertificateCredential(
                //         aadAuthOptions.AADTenantId,
                //         aadAuthOptions.AADClientId,
                //         certificate,
                //         certOptions);
                // });
            }
        });
    }
}