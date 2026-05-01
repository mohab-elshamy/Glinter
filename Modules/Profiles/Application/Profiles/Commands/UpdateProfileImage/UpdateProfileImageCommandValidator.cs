namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateProfileImage;

public class UpdateProfileImageCommandValidator
{
    public List<string> Validate(UpdateProfileImageCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.ProfileImageUrl))
            errors.Add("Profile image URL is required.");

        if (command.ProfileImageUrl.Length > 1000)
            errors.Add("Profile image URL must not exceed 1000 characters.");

        if (!Uri.TryCreate(command.ProfileImageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Profile image URL must be a valid HTTP or HTTPS URL.");
        }

        return errors;
    }
}