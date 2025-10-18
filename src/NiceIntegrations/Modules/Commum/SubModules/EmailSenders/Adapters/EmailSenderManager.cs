
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Dtos;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters;

/// <summary>
/// This class is responsible for managing the email senders
/// If one Fail another one is tried
/// If all fail an AggregateException is thrown with all the exceptions
/// </summary>
public class EmailSenderManager(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailSenderManager> logger
        ) : IEmailSenderPort
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<EmailSenderManager> _logger = logger;

    public Task<SendSimpleEmailResponse> SendSimpleEmailAsync(
        SendSimpleEmailRequest req,
        CancellationToken cancellationToken = default)
    {
        if (EmailSendersModule.AvailibleEmailSenders.Count is 0)
        {
            throw new ArgumentException("No adapter found");
        }

        using var scope = _scopeFactory.CreateScope();

        var exceptions = new List<Exception>();

        foreach (var sender in EmailSendersModule.AvailibleEmailSenders)
        {
            var adapter = scope.ServiceProvider.GetKeyedService<IEmailSenderPort>(sender.ToString());

            if (adapter is null)
            {
                if (exceptions.Count == 0)
                {
                    throw new ArgumentException($"No adapter found for {sender}");
                }

                throw new AggregateException(exceptions);
            }

            try
            {
                var response = adapter.SendSimpleEmailAsync(req, cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogWarning(ex, "Fail while sending email with {Sender}", sender);
            }
        }

        if (exceptions.Count == 0)
        {
            throw new Exception("No adapter found");
        }

        throw new AggregateException(exceptions);
    }
}