using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pickadate.Application.Safety.Commands;
using Pickadate.Application.Safety.Dtos;
using Pickadate.Application.Safety.Queries;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/safety-checks")]
public class SafetyChecksController : ControllerBase
{
    private readonly IMediator _mediator;
    public SafetyChecksController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a friend safety check for an accepted invitation.</summary>
    [Authorize]
    [HttpPost("invitations/{slug}")]
    public async Task<ActionResult<SafetyCheckDto>> Create(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSafetyCheckCommand(slug), ct);
        return Ok(result);
    }

    /// <summary>User presses "all good" to clear the scheduled alert early.</summary>
    [Authorize]
    [HttpPost("invitations/{slug}/confirm")]
    public async Task<IActionResult> Confirm(string slug, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmSafetyCheckCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Public view for the friend holding the shareable token.</summary>
    [AllowAnonymous]
    [HttpGet("{friendToken}")]
    public async Task<ActionResult<FriendSafetyViewDto>> GetByFriendToken(string friendToken, CancellationToken ct)
    {
        var view = await _mediator.Send(new GetFriendSafetyViewQuery(friendToken), ct);
        return view is null ? NotFound() : Ok(view);
    }
}
