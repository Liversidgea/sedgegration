using Microsoft.AspNetCore.Mvc;
using Sedgegration.Web.Services;

namespace Sedgegration.Web.ViewComponents;

public class ServiceStatusViewComponent(ManagementApiClient api) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var status = await api.CheckHealthAsync();
        return View(status);
    }
}
