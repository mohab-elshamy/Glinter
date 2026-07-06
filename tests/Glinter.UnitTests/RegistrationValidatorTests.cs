using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;
using Glinter.Modules.IdentityAccess.Domain.Constants;

namespace Glinter.UnitTests;

public sealed class RegistrationValidatorTests
{
    [Fact]
    public void Public_registration_rejects_privileged_and_unknown_roles()
    {
        // Arrange
        var validator = new RegisterCommandValidator();

        // Act
        var adminErrors = validator.Validate(ValidCommand(RoleNames.Admin));
        var unknownErrors = validator.Validate(ValidCommand("SuperUser"));

        // Assert
        Assert.Contains("Invalid public registration role.", adminErrors);
        Assert.Contains("Invalid public registration role.", unknownErrors);
    }

    [Fact]
    public void Registration_reports_all_missing_required_identity_fields()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            FullName = " ",
            Email = string.Empty,
            Password = " ",
            Role = string.Empty
        };

        // Act
        var errors = validator.Validate(command);

        // Assert
        Assert.Contains("Full name is required.", errors);
        Assert.Contains("Email is required.", errors);
        Assert.Contains("Password is required.", errors);
        Assert.Contains("Role is required.", errors);
    }

    private static RegisterCommand ValidCommand(string role) => new()
    {
        FullName = "Unit Test User",
        Email = "unit@glinter.test",
        Password = "UnitPassword!2026",
        Role = role
    };
}
