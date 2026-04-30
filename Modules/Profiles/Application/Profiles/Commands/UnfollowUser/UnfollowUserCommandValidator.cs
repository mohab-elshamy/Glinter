namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UnfollowUser;

public class UnfollowUserCommandValidator
{
    public List<string> Validate(UnfollowUserCommand command)
    {
        var errors = new List<string>();

        if (command.FollowedUserId == Guid.Empty)
            errors.Add("Followed user id is required.");

        return errors;
    }
}