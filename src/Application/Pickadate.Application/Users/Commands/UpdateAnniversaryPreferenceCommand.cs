using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Users.Commands;

public record UpdateAnniversaryPreferenceCommand(bool Enabled) : ICommand;

public class UpdateAnniversaryPreferenceCommandHandler : IRequestHandler<UpdateAnniversaryPreferenceCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public UpdateAnniversaryPreferenceCommandHandler(
        IUserRepository users,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(UpdateAnniversaryPreferenceCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        user.SetAnniversaryEnabled(request.Enabled);
        await _uow.CommitAsync(ct);
    }
}
