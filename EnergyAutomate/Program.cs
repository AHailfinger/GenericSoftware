global using EnergyAutomate.Components;
global using EnergyAutomate.Components.Account;
global using EnergyAutomate.Data;
global using EnergyAutomate.Growatt;
global using EnergyAutomate.Services;
global using EnergyAutomate.Services.CodeFactory;
global using EnergyAutomate.Tibber;
global using EnergyAutomate.Watchdogs;
using CoordinateSharp;
using EnergyAutomate.Definitions;
using EnergyAutomate.Emulator;
using EnergyAutomate.Emulator.Growatt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;

namespace EnergyAutomate;

public class Program
{
    #region Public Methods

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add API Controllers
        builder.Services.AddControllers();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Logging.AddConsole();

        // Konfiguration laden
        var configuration = builder.Configuration;
        var TraceEnabled = configuration.GetSection("Trace").GetValue<bool>("TraceEnabled");

        // Log all categories - remove category filter to see everything
        builder.Services.AddSingleton(sp => new CustomLoggerProvider(sp, LogLevel.Trace, category => true));

        if (TraceEnabled)
        {
            builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<CustomLoggerProvider>());
            builder.Services.AddSingleton(sp => new ILoggerTraceListener(sp));
        }

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
            .AddIdentityCookies();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


        builder.Services.AddTransient(sp =>
        {
            var latitudeString = builder.Configuration["ApiSettings:Latitude"];
            var longitudeString = builder.Configuration["ApiSettings:Longitude"];

            if (!double.TryParse(latitudeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
            {
                latitude = 0.0;
            }

            if (!double.TryParse(longitudeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                longitude = 0.0;
            }

            return new Coordinate(latitude, longitude, DateTime.Now);
        });

        builder.Services.AddTransient(sp => new GrowattApiClient("https://openapi.growatt.com", builder.Configuration["ApiSettings:GrowattApiToken"] ?? string.Empty));
        builder.Services.AddTransient(sp => new TibberApiClient(builder.Configuration["ApiSettings:TibberApiToken"] ?? string.Empty, new ProductInfoHeaderValue("EnergyAutomate", "1.0")));
        builder.Services.AddTransient(sp => new SmlParser(builder.Configuration["ApiSettings:TibberBridgeHost"] ?? string.Empty, builder.Configuration["ApiSettings:TibberBridgePassword"] ?? string.Empty));

        builder.Services.AddSingleton<PythonWrapper>();
        builder.Services.AddSingleton<GrowattModbusCodec>();
        builder.Services.AddSingleton<EnergyAutomateService>();
        builder.Services.AddSingleton<TIbberApiService>();
        builder.Services.AddSingleton<ApiQueueWatchdog<IDeviceQuery>>();
        builder.Services.AddSingleton<ICodeTemplateProvider, DefaultCodeTemplateProvider>();
        builder.Services.AddScoped<DatabaseCodeTemplateStore>();
        builder.Services.AddScoped<RuntimeCodeTemplateStore>();
        builder.Services.AddSingleton<RoslynCodeFactory>();
        builder.Services.AddScoped<RuntimeCodeTemplateExecutor>();

        builder.Services.AddSingleton<GrowattApiService>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EnergyAutomateService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<TIbberApiService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<GrowattApiService>());

        builder.Services.AddBlazorBootstrap();

        var app = builder.Build();

        if (TraceEnabled)
        {
            Trace.Listeners.Add(app.Services.GetRequiredService<ILoggerTraceListener>());
        }

        // Capture unexpected process-level failures for diagnostics.
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                app.Services.GetService<ILogger<Program>>()?.LogCritical(exception, "Unhandled domain exception");
            }
            catch
            {
                // Process-level exception handlers must never throw.
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            try
            {
                app.Services.GetService<ILogger<Program>>()?.LogError(e.Exception, "Unobserved task exception");
                e.SetObserved();
            }
            catch
            {
                // Process-level exception handlers must never throw.
            }
        };

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Apply database migrations defensively so startup diagnostics stay visible.
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Starting database migration");

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();

                logger.LogInformation("Database migration finished successfully");

                // Initialize code templates from database
                logger.LogInformation("Initializing code templates from database");
                var templateStore = scope.ServiceProvider.GetRequiredService<RuntimeCodeTemplateStore>();
                await templateStore.InitializeAsync(scope.ServiceProvider.GetRequiredService<CancellationTokenSource>().Token);
                logger.LogInformation("Code templates initialized successfully");
            }
        }
        catch (Exception ex)
        {
            app.Services.GetService<ILogger<Program>>()?.LogError(ex, "Database migration or template initialization failed. Continuing startup without applying changes.");
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        app.Run();
    }

    #endregion Public Methods
}
