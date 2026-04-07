using Sedgegration.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<ManagementApiClient>(http =>
{
    var baseAddress = builder.Configuration["ServiceApi:BaseAddress"]
        ?? "http://localhost:5000";
    http.BaseAddress = new Uri(baseAddress);
});
//testing
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
