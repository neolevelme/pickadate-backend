using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;

namespace Pickadate.Application.Users.Commands;

public record DeleteMyAccountCommand : ICommand;

/// <summary>
/// Spec §12 "Obriši nalog" — one click removes everything tied to the
/// caller. The handler is defined in Infrastructure because the delete
/// cascade is wide enough that a repository-per-aggregate indirection
/// would hurt more than it helps. See `DeleteMyAccountCommandHandler` in
/// the Infrastructure project.
/// </summary>
public interface IDeleteMyAccountService
{
    Task ExecuteAsync(Guid userId, CancellationToken ct);
}

public class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand>
{
    private readonly IDeleteMyAccountService _service;
    private readonly ICurrentUser _currentUser;

    public DeleteMyAccountCommandHandler(IDeleteMyAccountService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public Task Handle(DeleteMyAccountCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        return _service.ExecuteAsync(userId, ct);
    }
}
