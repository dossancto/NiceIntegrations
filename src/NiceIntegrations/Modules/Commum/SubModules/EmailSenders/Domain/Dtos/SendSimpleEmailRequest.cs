namespace NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Dtos;

public record SendSimpleEmailRequest
(
    string TargetEmail,
    string Subject,
    string Message
);

public record SendSimpleEmailResponse
(
    bool Ok
);