namespace Pickadate.Infrastructure.Services;

public class PushOptions
{
    public const string SectionName = "Push";

    /// <summary>VAPID public key (base64url). Shared with the frontend so browsers can subscribe.</summary>
    public string PublicKey { get; set; } = "";

    /// <summary>VAPID private key (base64url). Stays on the server.</summary>
    public string PrivateKey { get; set; } = "";

    /// <summary>Required by the VAPID spec — "mailto:..." or an https URL.</summary>
    public string Subject { get; set; } = "mailto:noreply@pickadate.me";
}
