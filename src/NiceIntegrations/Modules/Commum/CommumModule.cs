using NiceIntegrations.Modules.Commum.SubModules.EmailSenders;

namespace NiceIntegrations.Modules.Commum;

public static class CommumModule
{
    /// <summary>
    /// This method is responsible for adding the commum module
    /// </summary>
    /// <param name="services">The service collection</param>
    public static IServiceCollection AddCommumModule(this IServiceCollection services)
    {
        services.AddEmailSendersModule();

        return services;
    }
}