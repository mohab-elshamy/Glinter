namespace Glinter.Modules.Profiles.Application.Profiles.Commands.FollowUser;

public class FollowUserCommandValidator
{
    public List<string> Validate(FollowUserCommand command)
    {
        var errors = new List<string>();

        if (command.FollowedUserId == Guid.Empty)
            errors.Add("Followed user id is required.");

        return errors;
    }
}