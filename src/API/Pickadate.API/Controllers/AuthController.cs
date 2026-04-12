using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pickadate.Application.Auth.Commands;
using Pickadate.Application.Auth.Dtos;

namespace Pickadate.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public record RequestCodeRequest(string Email);
    public record VerifyCodeRequest(string Email, string Code);

    /// <summary>
    /// Generates a fresh 6-digit code and emails it to the address.
    /// Always returns 204 — the response does not reveal whether an account exists.
    /// </summary>
    [HttpPost("request-code")]
    public async Task<IActionResult> RequestCode([FromBody] RequestCodeRequest body, CancellationToken ct)
    {
        await _mediator.Send(new RequestCodeCommand(body.Email), ct);
        return NoContent();
    }

    /// <summary>
    /// Exchanges an email + 6-digit code for a JWT. Creates the account
    /// on first successful verify (lazy registration).
    /// </summary>
    [HttpPost("verify-code")]
    public async Task<ActionResult<AuthResponse>> VerifyCode([FromBody] VerifyCodeRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyCodeCommand(body.Email, body.Code), ct);
        return Ok(result);
    }
}
