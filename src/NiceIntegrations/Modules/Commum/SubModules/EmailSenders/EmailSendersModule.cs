using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters.LoggerEmailSender;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters.MailgunIntegration;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Enums;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders;

public static class EmailSendersModule
{
    public static List<EmailSenderType> AvailibleEmailSenders { get; private set; } = [];

    /// <summary>
    /// This method is responsible for adding the email senders module
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEmailSendersModule(this IServiceCollection services)
    {
        services.AddEmailSendersIntegrations();

        // This should not be KeyedService as it will handle all other services
        services.AddTransient<IEmailSenderPort, EmailSenderManager>();

        return services;
    }

    /// <summary>
    /// This method is responsible for adding the email senders integrations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    private static IServiceCollection AddEmailSendersIntegrations(this IServiceCollection services)
    {
        // The order is important as it adds the adapters in the order they are added
        //
        // You can also add conditionally the adapters based on the environment variable
        // Something like:
        // if (Environment.GetEnvironmentVariable("ENABLE_LOG_EMAIL_SENDER")?.ToLower() is "true")
        // {
        //     services.AddLoggerEmailSenderAdapter();
        // }

        services.AddLoggerEmailSenderAdapter();

        services.AddMailgunEmailSenderAdapter();

        return services;
    }

    /// <summary>
    /// This method is responsible for adding the logger email sender adapter
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    private static IServiceCollection AddLoggerEmailSenderAdapter(this IServiceCollection services)
    {
        services.AddKeyedTransient<IEmailSenderPort, LoggerEmailSenderAdapter>(EmailSenderType.Logger.ToString());
        AvailibleEmailSenders.Add(EmailSenderType.Logger);
        return services;
    }

    private static IServiceCollection AddMailgunEmailSenderAdapter(this IServiceCollection services)
    {
        services.AddHttpClient<MailGunEmailAdapter>(client =>
        {
            // Example of how to use the Mailgun
            // client.BaseAddress = new Uri("https://api.mailgun.net/v3/sandbox.mailgun.org/messages");
        });

        services.AddKeyedTransient<IEmailSenderPort, MailGunEmailAdapter>(EmailSenderType.Mailgun.ToString());
        AvailibleEmailSenders.Add(EmailSenderType.Mailgun);

        return services;
    }
}