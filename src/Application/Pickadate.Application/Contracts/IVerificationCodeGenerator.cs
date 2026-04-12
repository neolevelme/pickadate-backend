namespace Pickadate.Application.Contracts;

public interface IVerificationCodeGenerator
{
    /// <summary>Generates a fresh 6-digit numeric verification code.</summary>
    string Generate();
}
