
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Dtos;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters.LoggerEmailSender;

/// <summary>
/// This is a simple adapter that logs the request and response
/// and returns a fake response
/// </summary>
public class LoggerEmailSenderAdapter(
        ILogger<LoggerEmailSenderAdapter> logger
    ) : IEmailSenderPort
{
    private readonly ILogger<LoggerEmailSenderAdapter> _logger = logger;

    public Task<SendSimpleEmailResponse> SendSimpleEmailAsync(SendSimpleEmailRequest req, CancellationToken cancellationToken = default)
    {
        if (req.Subject is "1")
        {
            throw new Exception("Fail the Logger provider");
        }

        _logger.LogInformation(
            "Sending email to {Email} with subject {Subject} and message {Message}",
            req.TargetEmail,
            req.Subject,
            req.Message);

        return Task.FromResult(new SendSimpleEmailResponse(true));
    }
}