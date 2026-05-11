namespace Glinter.Modules.Stays.Application.Listings.Commands;

public class SetStayActiveStatusCommand
{
    public Guid StayId { get; set; }
    public bool IsActive { get; set; }
}