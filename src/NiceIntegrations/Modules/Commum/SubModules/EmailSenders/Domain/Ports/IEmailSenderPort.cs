using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Dtos;

namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

/// <summary>
/// The port for sending emails
/// </summary>
public interface IEmailSenderPort
{
    /// <summary>
    /// Sends an email to the specified target email
    /// </summary>
    /// <param name="req">The request to send</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response</returns>
    Task<SendSimpleEmailResponse> SendSimpleEmailAsync(SendSimpleEmailRequest req, CancellationToken cancellationToken = default);
}