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

    /// <summary>
    /// Anonymous-first invitation creation. When the caller is logged in the
    /// invitation is attached to their account; otherwise the response carries
    /// a one-time owner token (recovery code) the browser stores under
    /// `pickadate.owner.&lt;slug&gt;`.
    /// </summary>
    [AllowAnonymous]
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

    public record ClaimBody(IReadOnlyList<string> OwnerTokens);

    /// <summary>
    /// Attach a list of anonymous invitations to the authenticated caller.
    /// The browser submits every owner token it has stashed in localStorage;
    /// the backend hashes each and claims any that still have a matching
    /// nullable initiator.
    /// </summary>
    [Authorize]
    [HttpPost("claim")]
    public async Task<ActionResult<ClaimInvitationsResult>> Claim(
        [FromBody] ClaimBody body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ClaimInvitationsCommand(body.OwnerTokens ?? Array.Empty<string>()), ct);
        return Ok(result);
    }

    // ---------- recipient actions (Phase 3) ----------

    /// <summary>Spec §4 Option 1. Requires the recipient to be authenticated.</summary>
    [Authorize]
    [HttpPost("{slug}/accept")]
    public async Task<IActionResult> Accept(string slug, CancellationToken ct)
    {
        await _mediator.Send(new AcceptInvitationCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Spec §4 Option 2. Requires auth; rejected after 3 rounds.</summary>
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

    /// <summary>Spec §4 Option 3. Anonymous allowed; IP-rate-limited (20/day).</summary>
    public record DeclineBody(string? Reason);

    [AllowAnonymous]
    [HttpPost("{slug}/decline")]
    public async Task<IActionResult> Decline(string slug, [FromBody] DeclineBody body, CancellationToken ct)
    {
        await _mediator.Send(new DeclineInvitationCommand(slug, body.Reason), ct);
        return NoContent();
    }

    // ---------- initiator actions ----------
    //
    // These three accept either a JWT (the user owns the invitation) or an
    // X-Invitation-Owner-Token header (anonymous bearer capability), so
    // anonymous creators can act on their own invitations from any browser
    // that holds the recovery code.

    [AllowAnonymous]
    [HttpPost("{slug}/cancel")]
    public async Task<IActionResult> Cancel(string slug, CancellationToken ct)
    {
        await _mediator.Send(new CancelInvitationCommand(slug), ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("{slug}/complete")]
    public async Task<IActionResult> Complete(string slug, CancellationToken ct)
    {
        await _mediator.Send(new MarkCompletedCommand(slug), ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("{slug}/accept-counter")]
    public async Task<IActionResult> AcceptCounter(string slug, CancellationToken ct)
    {
        await _mediator.Send(new AcceptCounterProposalCommand(slug), ct);
        return NoContent();
    }
}
