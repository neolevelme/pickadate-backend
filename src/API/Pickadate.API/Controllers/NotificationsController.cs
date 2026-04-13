using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pickadate.Application.Notifications.Commands;
using Pickadate.Infrastructure.Services;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PushOptions _pushOptions;

    public NotificationsController(IMediator mediator, IOptions<PushOptions> pushOptions)
    {
        _mediator = mediator;
        _pushOptions = pushOptions.Value;
    }

    /// <summary>VAPID public key so the browser can call pushManager.subscribe().</summary>
    [AllowAnonymous]
    [HttpGet("vapid-public-key")]
    public ActionResult<VapidPublicKeyDto> GetVapidPublicKey()
    {
        return Ok(new VapidPublicKeyDto(_pushOptions.PublicKey));
    }

    public record VapidPublicKeyDto(string PublicKey);
    public record SubscribeBody(string Endpoint, string P256dh, string Auth);
    public record UnsubscribeBody(string Endpoint);

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeBody body, CancellationToken ct)
    {
        await _mediator.Send(new SubscribeToPushCommand(body.Endpoint, body.P256dh, body.Auth), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeBody body, CancellationToken ct)
    {
        await _mediator.Send(new UnsubscribeFromPushCommand(body.Endpoint), ct);
        return NoContent();
    }
}
