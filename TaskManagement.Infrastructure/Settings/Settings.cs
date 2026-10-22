namespace TaskManagement.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskManagementAPI";
    public string Audience { get; set; } = "TaskManagementUI";
    /// <summary>HMAC-SHA256 signing key; must be at least 32 characters.</summary>
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>When false, emails are written to the log instead of being sent (mock notifications).</summary>
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "no-reply@taskmanager.local";
    public string FromName { get; set; } = "Task Manager";
}
