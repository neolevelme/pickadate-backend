namespace Pickadate.Application.Contracts;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default);
}
