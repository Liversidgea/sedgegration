namespace Sedgegration.Web.Models;

public record ServiceStatusViewModel
{
    public bool IsOnline { get; init; }
    public string? ErrorMessage { get; init; }
}
