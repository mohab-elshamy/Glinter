using System.Net;
using System.Net.Mail;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Email;

public sealed class IdentityEmailOptions
{
    public const string SectionName = "IdentityEmail";
    public string BaseUrl { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Glinter";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool EnableSsl { get; set; } = true;
}

public sealed class IdentityEmailSender : IIdentityEmailSender
{
    private readonly IdentityEmailOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IdentityEmailSender> _logger;

    public IdentityEmailSender(
        IOptions<IdentityEmailOptions> options,
        IWebHostEnvironment environment,
        ILogger<IdentityEmailSender> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task SendConfirmationAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken = default)
    {
        var link = BuildLink(
            "/confirm-email",
            user.Id,
            token);
        return SendAsync(
            user.Email!,
            "Confirm your Glinter email",
            $"Confirm your email by opening: {link}",
            cancellationToken);
    }

    public Task SendPasswordResetAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken = default)
    {
        var link = BuildLink(
            "/reset-password",
            user.Id,
            token,
            user.Email);
        return SendAsync(
            user.Email!,
            "Reset your Glinter password",
            $"Reset your password by opening: {link}",
            cancellationToken);
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            _logger.LogInformation(
                "Identity email '{Subject}' generated for {Recipient}; SMTP is not configured in Development.",
                subject,
                recipient);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl
        };
        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(
                _options.SmtpUsername,
                _options.SmtpPassword);
        }
        await client.SendMailAsync(message, cancellationToken);
    }

    private string BuildLink(
        string path,
        Guid userId,
        string token,
        string? email = null)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var emailQuery = string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : $"&email={Uri.EscapeDataString(email)}";
        return $"{_options.BaseUrl.TrimEnd('/')}{path}{separator}userId={userId:D}&token={Uri.EscapeDataString(token)}{emailQuery}";
    }
}
