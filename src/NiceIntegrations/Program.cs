using Microsoft.AspNetCore.Mvc;

using NiceIntegrations.Modules.Commum;
using NiceIntegrations.Modules.Commum.SubModules.EmailSenders.Domain.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services
    .AddCommumModule()
    ;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// this tests sending email with fallback Providers
// Send 1 to fail logger provider, should fallback to mailgun
// Send 2 to fail both logger and mailgun provider
// etc...
app.MapGet("/send-email", async (
        [FromQuery] string error,
        [FromServices] IEmailSenderPort emailSender
    ) =>
{
    await emailSender.SendSimpleEmailAsync(new(
        TargetEmail: "test@test.com",
        Subject: error,
        Message: "This is a test email"
    ));

    return new
    {
        Message = "Email sent"
    };

});

app.UseHttpsRedirection();

app.Run();