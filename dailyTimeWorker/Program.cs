using dailyTimeWorker.Configuration;
using dailyTimeWorker.Services.Chrome;
using dailyTimeWorker.Services.DailyTimeApi;
using dailyTimeWorker.Services.Notifications;
using dailyTimeWorker.Services.Scraping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DailyTimeApiOptions>(
    builder.Configuration.GetSection(DailyTimeApiOptions.SectionName));
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.Configure<NotificationsOptions>(
    builder.Configuration.GetSection(NotificationsOptions.SectionName));

var corsOrigins = builder.Configuration["Cors:Origins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:4010"];

builder.Services.AddCors(o => o.AddPolicy("DevFront", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "DailyTime Worker",
        Version = "v1",
        Description = "Jobs de fondo: scraper de portales, notificaciones y correo."
    });
});

builder.Services.AddHttpClient<IDailyTimeApiClient, DailyTimeApiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DailyTimeApiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(60);
}).ConfigurePrimaryHttpMessageHandler(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DailyTimeApiOptions>>().Value;
    var handler = new HttpClientHandler();
    if (opts.IgnoreSslErrors)
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

builder.Services.AddSingleton<IPortalScrapeEngine, PortalScrapeEngine>();
builder.Services.AddSingleton<IScrapeRunCoordinator, ScrapeRunCoordinator>();
builder.Services.AddSingleton<IChromeDebugLauncher, ChromeDebugLauncher>();
builder.Services.AddScoped<IPortalScrapeService, PortalScrapeService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddHostedService<PortalScrapeBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS antes del redirect: un OPTIONS preflight no puede seguir redirects (rompe Capturar desde :4010).
app.UseCors("DevFront");

var disableHttpsRedirect = app.Configuration.GetValue(
    "DisableHttpsRedirection",
    app.Environment.IsDevelopment());
if (!disableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
