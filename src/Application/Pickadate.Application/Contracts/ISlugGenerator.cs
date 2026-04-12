namespace Pickadate.Application.Contracts;

public interface ISlugGenerator
{
    /// <summary>Generates an unguessable short code in the `xx-yyyy` format (spec §3 korak 5).</summary>
    string Generate();
}
