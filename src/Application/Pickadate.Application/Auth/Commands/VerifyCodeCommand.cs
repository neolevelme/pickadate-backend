using FluentValidation;
using MediatR;
using Pickadate.Application.Auth.Dtos;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Auth;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Auth.Commands;

public record VerifyCodeCommand(string Email, string Code) : ICommand<AuthResponse>;

public class VerifyCodeCommandValidator : AbstractValidator<VerifyCodeCommand>
{
    public VerifyCodeCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches("^[0-9]{6}$")
            .WithMessage("Code must be 6 digits.");
    }
}

public class VerifyCodeCommandHandler : IRequestHandler<VerifyCodeCommand, AuthResponse>
{
    private readonly IVerificationCodeRepository _codes;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _uow;

    public VerifyCodeCommandHandler(
        IVerificationCodeRepository codes,
        IUserRepository users,
        IJwtTokenService jwt,
        IUnitOfWork uow)
    {
        _codes = codes;
        _users = users;
        _jwt = jwt;
        _uow = uow;
    }

    public async Task<AuthResponse> Handle(VerifyCodeCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var active = await _codes.GetActiveAsync(email, ct)
            ?? throw new InvalidCredentialsException("No active verification code. Request a new one.");

        if (!active.IsUsable(DateTime.UtcNow))
        {
            throw new InvalidCredentialsException("Verification code expired. Request a new one.");
        }

        if (!CryptoEquals(active.Code, request.Code))
        {
            throw new InvalidCredentialsException("Invalid verification code.");
        }

        active.MarkUsed();

        // Lazy registration: first successful verify creates the account.
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
        {
            user = User.Create(email);
            await _users.AddAsync(user, ct);
        }
        user.RecordLogin();

        await _uow.CommitAsync(ct);

        var (token, expires) = _jwt.Issue(user);
        return new AuthResponse(
            token,
            expires,
            new AuthUserDto(user.Id, user.Email, user.Name, user.Role.ToString()));
    }

    // Constant-time string comparison to avoid timing oracles against short codes.
    private static bool CryptoEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message) { }
}
