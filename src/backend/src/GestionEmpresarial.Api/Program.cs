using GestionEmpresarial.Api.Services;
using GestionEmpresarial.Api.Filters;
using GestionEmpresarial.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

const string BootstrapAdminArgument = "--bootstrap-admin";
var bootstrapAdminRequested = args.Contains(BootstrapAdminArgument, StringComparer.Ordinal);
var builderArguments = args.Where(argument =>
    !string.Equals(argument, BootstrapAdminArgument, StringComparison.Ordinal)).ToArray();

var builder = WebApplication.CreateBuilder(builderArguments);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-GestionEmpresarial.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.LoginPath = "/api/v1/auth/login";
    options.AccessDeniedPath = "/api/v1/auth/forbidden";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events ??= new CookieAuthenticationEvents();
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-GestionEmpresarial.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});
builder.Services.AddControllers(options =>
    options.Filters.AddService<AntiforgeryAuthorizationFilter>());
builder.Services.AddScoped<AntiforgeryAuthorizationFilter>();
builder.Services.AddScoped<IdentityBootstrapper>();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var identityBootstrapper = scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>();
    await identityBootstrapper.SeedRolesAsync();

    if (bootstrapAdminRequested)
    {
        var created = await identityBootstrapper.CreateFirstAdministratorAsync();
        Console.WriteLine(created
            ? "Bootstrap administrator created. Remove the temporary BootstrapAdmin secrets when finished."
            : "The configured administrator already exists; no changes were made.");
        return;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();

public partial class Program { }
