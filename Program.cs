using Microsoft.EntityFrameworkCore;
using Radzen;
using SmartPotsWeb.Components;
using SmartPotsWeb.Endpoints;
using SmartPotsWeb.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHub<TelemetryHub>("/telemetryHub");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapTelemetryEndpoints();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("База даних успішно оновлена!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка міграції: {ex.Message}");
    }
}

app.Run();
