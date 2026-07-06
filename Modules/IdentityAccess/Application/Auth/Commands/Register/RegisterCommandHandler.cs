using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;

public class RegisterCommandHandler
{
    private static readonly string[] RolesRequiringReview =
    [
        RoleNames.LocalBuddy,
        RoleNames.HotelOwner,
        RoleNames.ExperienceProvider
    ];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityEmailSender _emailSender;
    private readonly IWebHostEnvironment _environment;
    private readonly RegisterCommandValidator _validator = new();

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IIdentityEmailSender emailSender,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _environment = environment;
    }

    public async Task<RegisterResponse> HandleAsync(RegisterCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new ConflictException("Email already exists.");

        var requiresReview = RolesRequiringReview.Contains(command.Role);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = command.FullName,
            Email = command.Email,
            UserName = command.Email,
            EmailConfirmed = false,
            IsActive = !requiresReview,
            AccountReviewStatus = requiresReview
                ? AccountReviewStatus.Pending
                : AccountReviewStatus.NotRequired,
            IdentityDocumentUrl = requiresReview ? command.IdentityDocumentUrl?.Trim() : null,
            IdentityDocumentFileName = requiresReview ? command.IdentityDocumentFileName?.Trim() : null,
            IdentityDocumentContentType = requiresReview ? command.IdentityDocumentContentType?.Trim() : null
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
            throw new ValidationException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        var roleResult = await _userManager.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new ValidationException(
                string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
        }

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _emailSender.SendConfirmationAsync(user, confirmationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Message = requiresReview
                ? "Registration submitted. Confirm your email, then wait for an administrator to review your identity document."
                : "Registration succeeded. Confirm your email before signing in.",
            DevelopmentConfirmationToken = _environment.IsDevelopment()
                ? confirmationToken
                : null
        };
    }
}
