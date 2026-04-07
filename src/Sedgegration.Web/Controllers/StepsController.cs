using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.Controllers;

public class StepsController(ManagementApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var steps = await api.GetStepsAsync();
        return View(steps);
    }

    [HttpPost]
    public async Task<IActionResult> Register(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        await api.RegisterStepAsync(name);
        return RedirectToAction("Index");
    }
}
