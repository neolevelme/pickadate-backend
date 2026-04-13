using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pickadate.Application.Users.Commands;
using Pickadate.Application.Users.Dtos;
using Pickadate.Application.Users.Queries;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Authenticated user profile, including preferences.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
    {
        var me = await _mediator.Send(new GetMeQuery(), ct);
        return Ok(me);
    }

    public record AnniversaryPreferenceBody(bool Enabled);

    /// <summary>Spec §8 toggle — opt in/out of anniversary reminders.</summary>
    [Authorize]
    [HttpPut("me/anniversary")]
    public async Task<IActionResult> UpdateAnniversaryPreference(
        [FromBody] AnniversaryPreferenceBody body,
        CancellationToken ct)
    {
        await _mediator.Send(new UpdateAnniversaryPreferenceCommand(body.Enabled), ct);
        return NoContent();
    }

    /// <summary>Spec §12 GDPR "delete my account" — wipes everything tied to the caller.</summary>
    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount(CancellationToken ct)
    {
        await _mediator.Send(new DeleteMyAccountCommand(), ct);
        return NoContent();
    }
}
