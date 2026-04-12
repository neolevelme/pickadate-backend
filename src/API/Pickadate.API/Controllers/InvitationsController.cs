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

    /// <summary>Creates and publishes a new invitation. Returns the shareable slug.</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CreateInvitationResult>> Create(
        [FromBody] CreateInvitationCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/invitations/{result.Slug}", result);
    }

    /// <summary>Public endpoint (no auth). Returns 404 if the slug is unknown.</summary>
    [AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<ActionResult<InvitationDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        var invitation = await _mediator.Send(new GetInvitationBySlugQuery(slug), ct);
        return invitation is null ? NotFound() : Ok(invitation);
    }
}
