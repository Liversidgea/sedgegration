using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.Controllers;

public class HomeController(ManagementApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var status = await api.CheckHealthAsync();
        return View(status);
    }
}
