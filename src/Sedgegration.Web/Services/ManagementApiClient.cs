using System.Net;
using System.Net.Http.Json;
using Sedgegration.Web.Models;

namespace Sedgegration.Web.Services;

/// <summary>
/// Typed HTTP client for the Sedgegration management API.
/// </summary>
public class ManagementApiClient(HttpClient http)
{
    // ── Health ─────────────────────────────────────────────────

    public async Task<ServiceStatusViewModel> CheckHealthAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync("/api/health", ct);
            response.EnsureSuccessStatusCode();
            return new ServiceStatusViewModel { IsOnline = true };
        }
        catch (HttpRequestException ex)
        {
            return new ServiceStatusViewModel { IsOnline = false, ErrorMessage = ex.Message };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ServiceStatusViewModel { IsOnline = false, ErrorMessage = "Request timed out" };
        }
    }

    // ── Workflows ────────────────────────────────────────────

    public async Task<IReadOnlyList<WorkflowViewModel>> GetWorkflowsAsync(
        CancellationToken ct = default)
    {
        var list = await http.GetFromJsonAsync<List<WorkflowViewModel>>(
            "/api/workflows", ct);
        return list ?? [];
    }

    public async Task<WorkflowViewModel?> GetWorkflowAsync(
        string id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/workflows/{Uri.EscapeDataString(id)}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowViewModel>(cancellationToken: ct);
    }

    public async Task<WorkflowViewModel> CreateWorkflowAsync(
        CreateWorkflowForm form, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/workflows", form, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowViewModel>(cancellationToken: ct))!;
    }

    public async Task<WorkflowViewModel> UpdateWorkflowAsync(
        string id, UpdateWorkflowForm form, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"/api/workflows/{Uri.EscapeDataString(id)}", form, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkflowViewModel>(cancellationToken: ct))!;
    }

    public async Task DeleteWorkflowAsync(
        string id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(
            $"/api/workflows/{Uri.EscapeDataString(id)}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ToggleResponse> ToggleWorkflowAsync(
        string id, CancellationToken ct = default)
    {
        var response = await http.PostAsync(
            $"/api/workflows/{Uri.EscapeDataString(id)}/toggle", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ToggleResponse>(cancellationToken: ct))!;
    }

    public async Task<ReloadResponse> ReloadWorkflowsAsync(
        CancellationToken ct = default)
    {
        var response = await http.PostAsync("/api/workflows/reload", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReloadResponse>(cancellationToken: ct))!;
    }

    // ── Protocols ────────────────────────────────────────────

    public async Task<ProtocolListResponse> GetProtocolsAsync(
        CancellationToken ct = default)
    {
        return (await http.GetFromJsonAsync<ProtocolListResponse>(
            "/api/protocols", ct))!;
    }

    public async Task<ProtocolStatusResponse> StartProtocolAsync(
        string name, CancellationToken ct = default)
    {
        var response = await http.PostAsync(
            $"/api/protocols/{Uri.EscapeDataString(name)}/start", null, ct);
        if (!response.IsSuccessStatusCode)
        {
            string msg;
            try
            {
                var obj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
                msg = obj != null && obj.TryGetValue("error", out var e) ? e : await response.Content.ReadAsStringAsync(cancellationToken: ct);
            }
            catch
            {
                msg = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            }
            throw new InvalidOperationException(msg ?? "Failed to start protocol");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProtocolStatusResponse>(cancellationToken: ct))!;
    }

    public async Task<ProtocolStatusResponse> StopProtocolAsync(
        string name, CancellationToken ct = default)
    {
        var response = await http.PostAsync(
            $"/api/protocols/{Uri.EscapeDataString(name)}/stop", null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProtocolStatusResponse>(cancellationToken: ct))!;
    }

    public async Task RegisterProtocolAsync(
        RegisterProtocolForm form, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/protocols", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            string msg;
            try
            {
                var obj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
                msg = obj != null && obj.TryGetValue("error", out var e) ? e : await response.Content.ReadAsStringAsync(cancellationToken: ct);
            }
            catch
            {
                msg = await response.Content.ReadAsStringAsync(cancellationToken: ct);
            }
            throw new InvalidOperationException(msg ?? "Failed to register protocol");
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task UnregisterProtocolAsync(
        string name, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(
            $"/api/protocols/{Uri.EscapeDataString(name)}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<string>> GetProtocolTypesAsync(
        CancellationToken ct = default)
    {
        var list = await http.GetFromJsonAsync<List<string>>(
            "/api/protocols/types", ct);
        return list ?? [];
    }

    // ── Steps ────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetStepsAsync(
        CancellationToken ct = default)
    {
        var list = await http.GetFromJsonAsync<List<string>>(
            "/api/steps", ct);
        return list ?? [];
    }

    // ── Requests ───────────────────────────────────────────

    public async Task<IReadOnlyList<RequestViewModel>> GetRequestsAsync(CancellationToken ct = default)
    {
        var list = await http.GetFromJsonAsync<List<RequestViewModel>>("/api/requests", ct);
        return list ?? [];
    }

    public async Task<RequestViewModel?> GetRequestAsync(string id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/requests/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RequestViewModel>(cancellationToken: ct);
    }

    public async Task RegisterStepAsync(string name, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/steps/register", new { Name = name }, ct);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to register step: {content}");
        }
    }
}
