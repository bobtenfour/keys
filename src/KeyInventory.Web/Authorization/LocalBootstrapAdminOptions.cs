namespace KeyInventory.Web.Authorization;

public sealed class LocalBootstrapAdminOptions
{
    public const string SectionName = "LocalBootstrapAdmin";

    public bool Enabled { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
