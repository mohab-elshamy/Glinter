using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.SecurityFlows;

public sealed class AccountRecoveryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityEmailSender _emailSender;
    private readonly IAuthTokenService _authTokenService;
    private readonly IWebHostEnvironment _environment;

    public AccountRecoveryHandler(
        UserManager<ApplicationUser> userManager,
        IIdentityEmailSender emailSender,
        IAuthTokenService authTokenService,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _authTokenService = authTokenService;
        _environment = environment;
    }

    public async Task<MessageResponse> ConfirmEmailAsync(
        ConfirmEmailRequest request)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token))
            throw new ValidationException("UserId and confirmation token are required.");

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                   ?? throw new ValidationException("Invalid confirmation request.");
        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            throw new ValidationException("Invalid or expired confirmation token.");

        return new MessageResponse { Message = "Email confirmed successfully." };
    }

    public async Task<MessageResponse> ResendConfirmationAsync(
        EmailRequest request)
    {
        var user = await FindEligibleUserAsync(request.Email, requireUnconfirmed: true);
        string? token = null;
        if (user is not null)
        {
            token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailSender.SendConfirmationAsync(user, token);
        }

        return Generic(
            "If the account exists and is unconfirmed, a confirmation email has been sent.",
            token);
    }

    public async Task<MessageResponse> ForgotPasswordAsync(EmailRequest request)
    {
        var user = await FindEligibleUserAsync(request.Email, requireUnconfirmed: false);
        string? token = null;
        if (user is not null && user.EmailConfirmed)
        {
            token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailSender.SendPasswordResetAsync(user, token);
        }

        return Generic(
            "If the account exists, a password-reset email has been sent.",
            token);
    }

    public async Task<MessageResponse> ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("Email, token, and new password are required.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email)
                   ?? throw new ValidationException("Invalid password-reset request.");
        var result = await _userManager.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(
                string.Join(" | ", result.Errors.Select(x => x.Description)));
        }

        await _authTokenService.RevokeAllAsync(user.Id, "Password reset");
        return new MessageResponse { Message = "Password reset successfully." };
    }

    private async Task<ApplicationUser?> FindEligibleUserAsync(
        string email,
        bool requireUnconfirmed)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return null;
        return requireUnconfirmed && user.EmailConfirmed ? null : user;
    }

    private MessageResponse Generic(string message, string? token) =>
        new()
        {
            Message = message,
            DevelopmentToken = _environment.IsDevelopment() ? token : null
        };
}

public sealed class MfaFlowHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMfaTicketService _ticketService;
    private readonly IAuthTokenService _authTokenService;

    public MfaFlowHandler(
        UserManager<ApplicationUser> userManager,
        IMfaTicketService ticketService,
        IAuthTokenService authTokenService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
        _authTokenService = authTokenService;
    }

    public async Task<MfaSetupResponse> SetupAsync(MfaTicketRequest request)
    {
        var user = await GetAdminAsync(request.MfaTicket, "setup");
        EnsureSucceeded(
            await _userManager.ResetAuthenticatorKeyAsync(user),
            "Authenticator setup failed.");
        var key = await _userManager.GetAuthenticatorKeyAsync(user)
                  ?? throw new InvalidOperationException("Authenticator key was not generated.");
        var enableTicket = await _ticketService.CreateAsync(user.Id, "enable");
        var issuer = Uri.EscapeDataString("Glinter");
        var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        return new MfaSetupResponse
        {
            SharedKey = key,
            AuthenticatorUri =
                $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6",
            MfaTicket = enableTicket.Value,
            ExpiresAtUtc = enableTicket.ExpiresAtUtc
        };
    }

    public async Task<EnableMfaResponse> EnableAsync(EnableMfaRequest request)
    {
        var user = await GetAdminAsync(request.MfaTicket, "enable");
        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            NormalizeCode(request.Code));
        if (!valid)
            throw new AuthenticationException("Invalid authenticator code.");

        EnsureSucceeded(
            await _userManager.SetTwoFactorEnabledAsync(user, true),
            "MFA could not be enabled.");
        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))
            ?.ToArray() ?? [];
        var roles = await _userManager.GetRolesAsync(user);
        return new EnableMfaResponse
        {
            Authentication = await _authTokenService.IssueAsync(user, roles),
            RecoveryCodes = recoveryCodes
        };
    }

    public async Task<AuthResponse> VerifyAsync(VerifyMfaRequest request)
    {
        var user = await GetAdminAsync(request.MfaTicket, "verify");
        var valid = false;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            valid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                NormalizeCode(request.Code));
        }
        else if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(
                user,
                request.RecoveryCode);
            valid = result.Succeeded;
        }

        if (!valid)
            throw new AuthenticationException("Invalid MFA code.");

        var roles = await _userManager.GetRolesAsync(user);
        return await _authTokenService.IssueAsync(user, roles);
    }

    private async Task<ApplicationUser> GetAdminAsync(string ticket, string purpose)
    {
        var userId = await _ticketService.ConsumeAsync(ticket, purpose);
        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new AuthenticationException("Invalid MFA ticket.");
        if (!user.IsActive || !user.EmailConfirmed ||
            !await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            throw new AuthenticationException("Invalid MFA ticket.");
        }
        return user;
    }

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new ConflictException(
                $"{message} {string.Join(" | ", result.Errors.Select(x => x.Description))}");
        }
    }
}
