namespace Pickadate.Domain.Invitations;

public enum InvitationStatus
{
    Draft = 0,
    Pending = 1,
    Viewed = 2,
    Accepted = 3,
    Countered = 4,
    Declined = 5,
    Cancelled = 6,
    Completed = 7,
    Expired = 8
}

public enum InvitationVibe
{
    Coffee = 0,
    Drinks = 1,
    Walk = 2,
    Activity = 3,
    Dinner = 4,
    Custom = 99
}
