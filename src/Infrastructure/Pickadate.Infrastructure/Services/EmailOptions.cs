namespace Pickadate.Infrastructure.Services;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string AppBaseUrl { get; set; } = "";
    public string FrontendBaseUrl { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
}
