using EthanTcm.Application;
using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Infrastructure;
using EthanTcm.Web.Authentication;
using EthanTcm.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.IISIntegration;
using System.IO.Compression;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

builder.WebHost.UseIISIntegration();

builder.Services.Configure<EthanTcmAuthenticationOptions>(
    builder.Configuration.GetSection(EthanTcmAuthenticationOptions.SectionName));
builder.Services.Configure<TaxDocumentStorageOptions>(
    builder.Configuration.GetSection(TaxDocumentStorageOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    var documentStorage = builder.Configuration
        .GetSection(TaxDocumentStorageOptions.SectionName)
        .Get<TaxDocumentStorageOptions>() ?? new TaxDocumentStorageOptions();

    options.MultipartBodyLengthLimit = documentStorage.MaxFileSizeBytes;
    options.ValueLengthLimit = 4096;
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddMemoryCache(options => options.SizeLimit = 1024);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditRequestContext, WebAuditRequestContext>();
builder.Services.AddTransient<IClaimsTransformation, ApplicationClaimsTransformation>();

var authenticationMode = builder.Configuration
    .GetSection(EthanTcmAuthenticationOptions.SectionName)
    .GetValue(nameof(EthanTcmAuthenticationOptions.Mode), builder.Environment.IsDevelopment() ? AuthMode.LocalAuth : AuthMode.WindowsAuth);

if (authenticationMode == AuthMode.LocalAuth)
{
    builder.Services
        .AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            options => { });
}
else
{
    builder.Services
        .AddAuthentication(IISDefaults.AuthenticationScheme)
        .AddNegotiate();
}

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var role in ApplicationRoles.All)
    {
        options.AddPolicy(role, policy => policy.RequireRole(role));
    }

    foreach (var permission in ApplicationPermissions.All)
    {
        options.AddPolicy(permission, policy => policy.RequireRole(ApplicationPermissions.RolesFor(permission)));
    }
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

if (args.Length > 0 && args[0].Equals("sync-tax-catalog", StringComparison.OrdinalIgnoreCase))
{
    var dryRun = args.Skip(1).Any(argument => argument.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
    using var scope = app.Services.CreateScope();
    var synchronizationService = scope.ServiceProvider.GetRequiredService<ITaxCatalogSynchronizationService>();
    var report = await synchronizationService.SynchronizeAsync(dryRun);
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

if (args.Length > 0 && args[0].Equals("seed-initial-tax-obligations", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IInitialTaxObligationSeeder>();
    var report = await seeder.SeedAsync();
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

if (app.Environment.IsDevelopment() &&
    app.Configuration.GetSection("SeedData").GetValue("RunInitialTaxObligationSeed", false))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IInitialTaxObligationSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("EthanTcm.Web.Security");

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Unhandled request error. TraceIdentifier={TraceIdentifier} Path={Path} User={User}",
            context.TraceIdentifier,
            context.Request.Path,
            context.User.Identity?.Name ?? "anonymous");
        throw;
    }
});
app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    var claimsTransformation = context.RequestServices.GetRequiredService<IClaimsTransformation>();
    context.User = await claimsTransformation.TransformAsync(context.User);
    await next();
});
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
