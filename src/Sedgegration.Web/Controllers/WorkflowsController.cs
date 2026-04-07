using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Models;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.Controllers;

public class WorkflowsController(ManagementApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var workflows = await api.GetWorkflowsAsync();
        return View(workflows);
    }

    public async Task<IActionResult> Details(string id)
    {
        var workflow = await api.GetWorkflowAsync(id);
        if (workflow is null) return NotFound();
        return View(workflow);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.AvailableSteps = await api.GetStepsAsync();
        return View(new CreateWorkflowForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkflowForm form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AvailableSteps = await api.GetStepsAsync();
            return View(form);
        }

        var created = await api.CreateWorkflowAsync(form);
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    public async Task<IActionResult> Edit(string id)
    {
        var workflow = await api.GetWorkflowAsync(id);
        if (workflow is null) return NotFound();

        ViewBag.AvailableSteps = await api.GetStepsAsync();

        var form = new UpdateWorkflowForm
        {
            Name = workflow.Name,
            Protocol = workflow.Protocol,
            Enabled = workflow.Enabled,
            Steps = workflow.Steps.Select(s => new StepForm
            {
                StepType = s.StepType,
                Order = s.Order,
                Config = s.Config
            }).ToList()
        };

        ViewBag.WorkflowId = id;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateWorkflowForm form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AvailableSteps = await api.GetStepsAsync();
            ViewBag.WorkflowId = id;
            return View(form);
        }

        await api.UpdateWorkflowAsync(id, form);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(string id)
    {
        var workflow = await api.GetWorkflowAsync(id);
        if (workflow is null) return NotFound();
        return View(workflow);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await api.DeleteWorkflowAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        await api.ToggleWorkflowAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reload()
    {
        await api.ReloadWorkflowsAsync();
        return RedirectToAction(nameof(Index));
    }
}
