using System.Text.Json;
using FluentValidation;
using Pickadate.Application.Auth.Commands;
using Pickadate.Application.Invitations.Commands;
using Pickadate.Application.Safety.Commands;
using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Validation failed", ex.Message);
        }
        catch (BusinessRuleValidationException ex)
        {
            await WriteProblem(context, StatusCodes.Status422UnprocessableEntity, "Business rule violation", ex.Details);
        }
        catch (InvalidCredentialsException ex)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (InvitationNotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
        }
        catch (TooManyDeclinesException ex)
        {
            await WriteProblem(context, StatusCodes.Status429TooManyRequests, "Too many requests", ex.Message);
        }
        catch (InvalidSafetyCheckStateException ex)
        {
            await WriteProblem(context, StatusCodes.Status422UnprocessableEntity, "Invalid state", ex.Message);
        }
        catch (CannotAcceptOwnInvitationException ex)
        {
            await WriteProblem(context, StatusCodes.Status422UnprocessableEntity, "Invalid state", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.");
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        var payload = new { type = $"https://httpstatuses.io/{status}", title, status, detail };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
