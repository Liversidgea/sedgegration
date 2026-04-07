using System.ComponentModel.DataAnnotations;

namespace Sedgegration.Web.Models;

// ── Read / display models ────────────────────────────────────

public record WorkflowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
    public List<StepViewModel> Steps { get; init; } = new();
    public bool Enabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record StepViewModel
{
    public string StepType { get; init; } = string.Empty;
    public int Order { get; init; }
    public Dictionary<string, object> Config { get; init; } = new();
}

// ── Form models (create / edit) ──────────────────────────────

public class CreateWorkflowForm
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Protocol { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public List<StepForm> Steps { get; set; } = new();
}

public class UpdateWorkflowForm
{
    [MaxLength(200)]
    public string? Name { get; set; }

    public string? Protocol { get; set; }

    public bool? Enabled { get; set; }

    public List<StepForm>? Steps { get; set; }
}

public class StepForm
{
    [Required]
    public string StepType { get; set; } = string.Empty;

    public int? Order { get; set; }

    public Dictionary<string, object>? Config { get; set; }
}
