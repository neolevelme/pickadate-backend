using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record CreateInvitationCommand(
    string Vibe,
    string? CustomVibe,
    string PlaceGoogleId,
    string PlaceName,
    string PlaceFormattedAddress,
    double PlaceLat,
    double PlaceLng,
    DateTime MeetingAtUtc,
    string? Message,
    string? MediaUrl) : ICommand<CreateInvitationResult>;

public class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    private static readonly string[] ValidVibes =
        ["Coffee", "Drinks", "Walk", "Activity", "Dinner", "Custom"];

    public CreateInvitationCommandValidator()
    {
        RuleFor(x => x.Vibe)
            .NotEmpty()
            .Must(v => ValidVibes.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Vibe must be one of: {string.Join(", ", ValidVibes)}.");

        When(x => string.Equals(x.Vibe, "Custom", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CustomVibe).NotEmpty().MaximumLength(64);
        });

        RuleFor(x => x.PlaceName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PlaceFormattedAddress).NotEmpty().MaximumLength(512);
        RuleFor(x => x.PlaceGoogleId).MaximumLength(256);
        RuleFor(x => x.PlaceLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.PlaceLng).InclusiveBetween(-180, 180);

        RuleFor(x => x.MeetingAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Meeting time must be in the future.");

        RuleFor(x => x.Message)
            .MaximumLength(140)
            .When(x => !string.IsNullOrWhiteSpace(x.Message));

        RuleFor(x => x.MediaUrl)
            .MaximumLength(512)
            .When(x => !string.IsNullOrWhiteSpace(x.MediaUrl));
    }
}

public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, CreateInvitationResult>
{
    private readonly IInvitationRepository _invitations;
    private readonly ISlugGenerator _slugs;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateInvitationCommandHandler(
        IInvitationRepository invitations,
        ISlugGenerator slugs,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _slugs = slugs;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<CreateInvitationResult> Handle(CreateInvitationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        var slug = await GenerateUniqueSlugAsync(ct);

        var vibe = Enum.Parse<InvitationVibe>(request.Vibe, ignoreCase: true);
        var place = Place.Create(
            request.PlaceGoogleId ?? "",
            request.PlaceName,
            request.PlaceFormattedAddress,
            request.PlaceLat,
            request.PlaceLng);

        var invitation = Invitation.CreateAndPublish(
            initiatorId: userId,
            slug: slug,
            vibe: vibe,
            customVibe: request.CustomVibe,
            place: place,
            meetingAtUtc: request.MeetingAtUtc,
            message: request.Message,
            mediaUrl: request.MediaUrl);

        await _invitations.AddAsync(invitation, ct);
        await _uow.CommitAsync(ct);

        return new CreateInvitationResult(slug);
    }

    // Retry a handful of times in the extraordinarily unlikely event of a slug collision.
    // With 26^2 * 36^4 ≈ 1.1B possibilities, a real collision at scale is still rare;
    // the loop just keeps correctness airtight instead of relying on probability.
    private async Task<string> GenerateUniqueSlugAsync(CancellationToken ct)
    {
        for (var i = 0; i < 5; i++)
        {
            var candidate = _slugs.Generate();
            if (!await _invitations.SlugExistsAsync(candidate, ct)) return candidate;
        }
        throw new InvalidOperationException("Failed to generate a unique slug after 5 attempts.");
    }
}
