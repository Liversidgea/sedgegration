using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Models;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.Controllers;

public class ProtocolsController(ManagementApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var protocols = await api.GetProtocolsAsync();
        return View(protocols);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(ProtocolConfigForm config)
    {
        if (!ModelState.IsValid)
        {
            var protocols = await api.GetProtocolsAsync();
            return View(nameof(Index), protocols);
        }

        try
        {
            await api.StartProtocolAsync(config.Protocol, config);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var protocols = await api.GetProtocolsAsync();
            return View(nameof(Index), protocols);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stop(string name)
    {
        await api.StopProtocolAsync(name);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Register()
    {
        ViewBag.AvailableTypes = await api.GetProtocolTypesAsync();
        ViewBag.AvailableWorkflows = await api.GetWorkflowsAsync();
        return View(new RegisterProtocolForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterProtocolForm form)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AvailableTypes = await api.GetProtocolTypesAsync();
            ViewBag.AvailableWorkflows = await api.GetWorkflowsAsync();
            return View(form);
        }

        try
        {
            await api.RegisterProtocolAsync(form);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.AvailableTypes = await api.GetProtocolTypesAsync();
            ViewBag.AvailableWorkflows = await api.GetWorkflowsAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unregister(string name)
    {
        await api.UnregisterProtocolAsync(name);
        return RedirectToAction(nameof(Index));
    }
}
