using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pickadate.Application.Invitations.Commands;
using Pickadate.Application.Invitations.Dtos;
using Pickadate.Application.Invitations.Queries;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvitationsController(IMediator mediator) => _mediator = mediator;

    // ---------- create / read ----------

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CreateInvitationResult>> Create(
        [FromBody] CreateInvitationCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/invitations/{result.Slug}", result);
    }

    /// <summary>Public endpoint: anyone with the slug can view the invitation.</summary>
    [AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<ActionResult<InvitationDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        var invitation = await _mediator.Send(new GetInvitationBySlugQuery(slug), ct);
        return invitation is null ? NotFound() : Ok(invitation);
    }

    /// <summary>Initiator-only: list of the caller's invitations, newest first.</summary>
    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<InvitationDetailDto>>> My(CancellationToken ct)
    {
        var list = await _mediator.Send(new GetMyInvitationsQuery(), ct);
        return Ok(list);
    }

    // ---------- recipient actions (Faza 3) ----------

    /// <summary>Spec §4 Opcija 1. Requires the recipient to be authenticated.</summary>
    [Authorize]
    [HttpPost("{slug}/accept")]
    public async Task<IActionResult> Accept(string slug, CancellationToken ct)
    {
        await _mediator.Send(new AcceptInvitationCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Spec §4 Opcija 2. Requires auth; rejected after 3 rounds.</summary>
    public record CounterProposeBody(
        DateTime? NewMeetingAtUtc,
        string? PlaceGoogleId,
        string? PlaceName,
        string? PlaceFormattedAddress,
        double? PlaceLat,
        double? PlaceLng);

    [Authorize]
    [HttpPost("{slug}/counter")]
    public async Task<IActionResult> CounterPropose(string slug, [FromBody] CounterProposeBody body, CancellationToken ct)
    {
        await _mediator.Send(new CounterProposeInvitationCommand(
            slug,
            body.NewMeetingAtUtc,
            body.PlaceGoogleId,
            body.PlaceName,
            body.PlaceFormattedAddress,
            body.PlaceLat,
            body.PlaceLng), ct);
        return NoContent();
    }

    /// <summary>Spec §4 Opcija 3. Anonymous allowed; IP-rate-limited (20/day).</summary>
    public record DeclineBody(string? Reason);

    [AllowAnonymous]
    [HttpPost("{slug}/decline")]
    public async Task<IActionResult> Decline(string slug, [FromBody] DeclineBody body, CancellationToken ct)
    {
        await _mediator.Send(new DeclineInvitationCommand(slug, body.Reason), ct);
        return NoContent();
    }

    // ---------- initiator actions (Faza 6) ----------

    /// <summary>Initiator cancels an invitation before it's accepted or completed.</summary>
    [Authorize]
    [HttpPost("{slug}/cancel")]
    public async Task<IActionResult> Cancel(string slug, CancellationToken ct)
    {
        await _mediator.Send(new CancelInvitationCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Initiator marks a completed meeting (spec §10).</summary>
    [Authorize]
    [HttpPost("{slug}/complete")]
    public async Task<IActionResult> Complete(string slug, CancellationToken ct)
    {
        await _mediator.Send(new MarkCompletedCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Initiator accepts the recipient's latest counter-proposal.</summary>
    [Authorize]
    [HttpPost("{slug}/accept-counter")]
    public async Task<IActionResult> AcceptCounter(string slug, CancellationToken ct)
    {
        await _mediator.Send(new AcceptCounterProposalCommand(slug), ct);
        return NoContent();
    }
}
