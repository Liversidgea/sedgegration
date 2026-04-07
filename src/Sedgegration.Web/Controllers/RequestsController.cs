using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Models;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.Controllers;

public class RequestsController : Controller
{
    private readonly ManagementApiClient _api;
    public RequestsController(ManagementApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var list = await _api.GetRequestsAsync();
        return View(list.ToList());
    }

    public async Task<IActionResult> Details(string id)
    {
        var req = await _api.GetRequestAsync(id);
        if (req is null) return NotFound();
        return View(req);
    }
}
