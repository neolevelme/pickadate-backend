namespace Pickadate.Application.Contracts;

public interface ISafetyTokenGenerator
{
    /// <summary>Generates an unguessable token for friend safety-check URLs.</summary>
    string Generate();
}
