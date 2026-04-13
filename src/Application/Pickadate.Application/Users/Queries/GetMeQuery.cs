using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Users.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Users.Queries;

public record GetMeQuery : IQuery<MeDto>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, MeDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;

    public GetMeQueryHandler(IUserRepository users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<MeDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        return new MeDto(
            user.Id, user.Email, user.Name, user.Country, user.VibePreference,
            user.ProfileImageUrl, user.Role.ToString(), user.AnniversaryEnabled, user.CreatedAt);
    }
}
