using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Auth;

namespace Pickadate.Application.Auth.Commands;

public record RequestCodeCommand(string Email) : ICommand;

public class RequestCodeCommandValidator : AbstractValidator<RequestCodeCommand>
{
    public RequestCodeCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}

public class RequestCodeCommandHandler : IRequestHandler<RequestCodeCommand>
{
    private readonly IVerificationCodeRepository _codes;
    private readonly IVerificationCodeGenerator _generator;
    private readonly IEmailService _email;
    private readonly IUnitOfWork _uow;

    public RequestCodeCommandHandler(
        IVerificationCodeRepository codes,
        IVerificationCodeGenerator generator,
        IEmailService email,
        IUnitOfWork uow)
    {
        _codes = codes;
        _generator = generator;
        _email = email;
        _uow = uow;
    }

    public async Task Handle(RequestCodeCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Supersede any outstanding unused codes so only the newest one works.
        await _codes.InvalidateOutstandingAsync(email, ct);

        var code = _generator.Generate();
        var verification = VerificationCode.Issue(email, code);

        await _codes.AddAsync(verification, ct);
        await _uow.CommitAsync(ct);

        await _email.SendVerificationCodeAsync(email, code, ct);
    }
}
