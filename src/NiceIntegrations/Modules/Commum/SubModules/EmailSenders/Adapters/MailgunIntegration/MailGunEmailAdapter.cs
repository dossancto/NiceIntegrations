
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Dtos;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Adapters.MailgunIntegration;

/// <summary>
/// This is a simple adapter that logs the request and response
/// and returns a fake response.
/// This is fake for Demo purposes
/// </summary>
public class MailGunEmailAdapter(
        ILogger<MailGunEmailAdapter> logger,
        HttpClient http
        ) : IEmailSenderPort
{
    private readonly ILogger<MailGunEmailAdapter> _logger = logger;

    // This is not used, but simulates a real cenario
    private readonly HttpClient _http = http;

    public Task<SendSimpleEmailResponse> SendSimpleEmailAsync(
        SendSimpleEmailRequest req,
        CancellationToken cancellationToken = default)
    {
        if(req.Subject is "2")
        {
            throw new Exception("Fail the Mailgun provider");
        }

        _logger.LogInformation(
            "[MAILGUN] Sending  email to {Email} with subject {Subject} and message {Message}",
            req.TargetEmail,
            req.Subject,
            req.Message);

        return Task.FromResult(new SendSimpleEmailResponse(true));
    }
}