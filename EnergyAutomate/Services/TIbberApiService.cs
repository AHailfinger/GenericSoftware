using System.Diagnostics;
using EnergyAutomate.Data;
using EnergyAutomate.Tibber;
using EnergyAutomate.Watchdogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnergyAutomate.Services;

public class TIbberApiService : IHostedService, IDisposable
{
    #region Private Fields

    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<TIbberApiService> logger;
    private bool isBusy;
    private bool isRunning;
    private string statusText = "Stopped";
    private string? lastError;

    #endregion Private Fields

    #region Public Constructors

    public TIbberApiService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        configuration = serviceProvider.GetRequiredService<IConfiguration>();
        logger = serviceProvider.GetRequiredService<ILogger<TIbberApiService>>();

        Trace.WriteLine("Create new TibberApiClient for watchdog ...", "Tibber");
    }

    #endregion Public Constructors

    #region Properties

    public Guid? TibberHomeId { get; set; }
    public TibberApiClient? TibberApiClient { get; private set; }
    public ThreadSafeObservableCollection<TibberPrice> TibberPrices { get; } = [];
    public ThreadSafeObservableCollection<TibberRealTimeMeasurement> TibberRealTimeMeasurement { get; } = [];
    public IObservable<RealTimeMeasurement>? RealTimeMeasurementListener { get; private set; }
    public IDisposable? RealTimeMeasurementObserver { get; private set; }
    public bool RestartRequested { get; set; }
    public bool IsBusy => isBusy;
    public bool IsRunning => isRunning;
    public string StatusText => statusText;
    public string? LastError => lastError;

    #endregion Properties

    #region Public Methods

    public async Task RestartListener()
    {
        await Task.Delay(5000);

        if (TibberHomeId.HasValue && TibberApiClient != null)
        {
            try
            {
                RealTimeMeasurementListener = null;
                RealTimeMeasurementObserver?.Dispose();
                RealTimeMeasurementObserver = null;
                TibberApiClient.Dispose();
                TibberApiClient = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tibber real-time listener restart cleanup failed");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Trace.WriteLine("StopRealTimeMeasurementListener finished ...", "Tibber");
            }

            await StartListener();
            RestartRequested = false;
        }
        else
        {
            await StartListener();
        }
    }

    public async Task TibberGetDataFromWeb()
    {
        try
        {
            var tibberApiClient = serviceProvider.GetRequiredService<TibberApiClient>();

            var basicData = await tibberApiClient.GetBasicData();
            TibberHomeId = basicData.Data.Viewer.Homes.FirstOrDefault()?.Id;

            if (TibberHomeId.HasValue)
            {
                await tibberApiClient.GetHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly);

                var customQueryBuilder =
                    new TibberQueryBuilder()
                        .WithAllScalarFields()
                        .WithViewer(
                            new ViewerQueryBuilder()
                                .WithAllScalarFields()
                                .WithAccountType()
                                .WithHome(
                                    new HomeQueryBuilder()
                                        .WithAllScalarFields()
                                        .WithAddress(new AddressQueryBuilder().WithAllFields())
                                        .WithCurrentSubscription(
                                            new SubscriptionQueryBuilder()
                                                .WithAllScalarFields()
                                                .WithSubscriber(new LegalEntityQueryBuilder().WithAllFields())
                                                .WithPriceInfo(new PriceInfoQueryBuilder().WithCurrent(new PriceQueryBuilder().WithAllFields()))
                                        )
                                        .WithOwner(new LegalEntityQueryBuilder().WithAllFields())
                                        .WithFeatures(new HomeFeaturesQueryBuilder().WithAllFields())
                                        .WithMeteringPointData(new MeteringPointDataQueryBuilder().WithAllFields()),
                                    TibberHomeId
                                )
                        );

                var customQuery = customQueryBuilder.Build();
                await tibberApiClient.Query(customQuery);

                var query = new TibberQueryBuilder().WithHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly, 12).Build();
                await tibberApiClient.Query(query);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message, "Tibber");
        }
    }

    public List<TibberPrice> TibberGetPriceDatas()
    {
        var items = TibberPrices.OrderByDescending(x => x.StartsAt).Take(48).ToList();
        return items.OrderBy(x => x.StartsAt).ToList();
    }

    public async Task TibberGetTomorrowPrices()
    {
        try
        {
            if (TibberHomeId.HasValue)
            {
                var customQueryBuilder =
                    new TibberQueryBuilder()
                        .WithViewer(
                            new ViewerQueryBuilder()
                                .WithHome(
                                    new HomeQueryBuilder()
                                        .WithCurrentSubscription(
                                            new SubscriptionQueryBuilder()
                                                .WithPriceInfo(
                                                    new PriceInfoQueryBuilder()
                                                        .WithAllScalarFields()
                                                        .WithToday(new PriceQueryBuilder().WithAllScalarFields())
                                                        .WithTomorrow(new PriceQueryBuilder().WithAllScalarFields())
                                                )
                                        ),
                                    TibberHomeId
                                )
                        );

                var customQuery = customQueryBuilder.Build();
                var result = await serviceProvider.GetRequiredService<TibberApiClient>().Query(customQuery);

                var tomorrowList = result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Tomorrow.Select(x => new TibberPrice()
                {
                    StartsAt = DateTimeOffset.Parse(x.StartsAt).ToUniversalTime(),
                    Total = x.Total,
                    Level = x.Level
                }).ToList();
                await TibberSavePricesAsync(tomorrowList);

                var todayList = result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Today.Select(x => new TibberPrice()
                {
                    StartsAt = DateTimeOffset.Parse(x.StartsAt).ToUniversalTime(),
                    Total = x.Total,
                    Level = x.Level
                }).ToList();
                await TibberSavePricesAsync(todayList);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message, "Tibber");
        }
    }

    public async Task TibberStoreRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        lock (TibberRealTimeMeasurement._syncRoot)
        {
            TibberRealTimeMeasurement.Add(tibberRealTimeMeasurement);
        }

        dbContext.TibberRealTimeMeasurements.Add(tibberRealTimeMeasurement);
        await dbContext.SaveChangesAsync();
    }

    public async Task TibberUpdateRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbEntity = await dbContext.TibberRealTimeMeasurements.FindAsync(tibberRealTimeMeasurement.TS);
        if (dbEntity != null)
        {
            dbContext.Entry(dbEntity).CurrentValues.SetValues(tibberRealTimeMeasurement);
            await dbContext.SaveChangesAsync();
        }
    }

    public List<TibberPrice> TibberListPrices()
    {
        lock (TibberPrices._syncRoot)
            return TibberPrices.ToList();
    }

    public List<TibberRealTimeMeasurement> TibberListRealTimeMeasurement()
    {
        lock (TibberRealTimeMeasurement._syncRoot)
            return TibberRealTimeMeasurement.ToList();
    }

    public void TibberRealTimeMeasurementRegisterOnCollectionChanged(object sender, Action callback) => TibberRealTimeMeasurement.RegisterOnCollectionChanged(sender, callback);

    public void TibberRealTimeMeasurementUnRegisterOnCollectionChanged(object sender) => TibberRealTimeMeasurement.UnRegisterOnCollectionChanged(sender);

    public async Task RestartServiceAsync(CancellationToken cancellationToken = default)
    {
        await StopServiceAsync(cancellationToken);
        await StartServiceAsync(cancellationToken, respectConfiguration: false);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartServiceAsync(cancellationToken, respectConfiguration: true);
    }

    public async Task StartListener(CancellationToken cancellationToken = default)
    {
        var token = configuration["ApiSettings:TibberApiToken"];
        if (string.IsNullOrWhiteSpace(token) || token.Contains("your-api-token", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Tibber API token is missing or placeholder. Real-time listener will not start.");
            return;
        }

        TibberApiClient ??= serviceProvider.GetRequiredService<TibberApiClient>();

        if (TibberApiClient != null)
        {
            if (!TibberHomeId.HasValue)
            {
                logger.LogTrace("Loading Tibber basic data");
                var basicData = await TibberApiClient.GetBasicData(cancellationToken);
                var homeId = basicData.Data?.Viewer?.Homes?.FirstOrDefault()?.Id;

                if (!homeId.HasValue)
                {
                    logger.LogWarning("Tibber returned no home id. Real-time listener will not start.");
                    return;
                }

                TibberHomeId = homeId.Value;
            }

            if (TibberHomeId.HasValue)
            {
                try
                {
                    logger.LogTrace("StartRealTimeMeasurementListener calling");
                    RealTimeMeasurementListener = await TibberApiClient.StartRealTimeMeasurementListener(TibberHomeId.Value, null!, cancellationToken);
                    RealTimeMeasurementObserver = RealTimeMeasurementListener.Subscribe(new RealTimeMeasurementObserver(this));
                    isRunning = true;
                    statusText = "Running";
                    logger.LogInformation("Tibber real-time measurement listener started for home {HomeId}", TibberHomeId.Value);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    statusText = "Stopped";
                    logger.LogInformation("Tibber real-time listener startup was canceled");
                }
                catch (Exception ex)
                {
                    statusText = "Failed";
                    lastError = ex.Message;
                    logger.LogError(ex, "Tibber real-time listener startup failed");
                }
            }
        }
    }

    public async Task StartServiceAsync(CancellationToken cancellationToken = default)
    {
        await StartServiceAsync(cancellationToken, respectConfiguration: false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopServiceAsync(cancellationToken);
    }

    public async Task StopListenerAsync(CancellationToken cancellationToken = default)
    {
        if (TibberApiClient != null && TibberHomeId.HasValue)
        {
            await TibberApiClient.StopRealTimeMeasurementListener(TibberHomeId.Value);
        }

        RealTimeMeasurementListener = null;
        RealTimeMeasurementObserver?.Dispose();
        RealTimeMeasurementObserver = null;
    }

    public async Task StopServiceAsync(CancellationToken cancellationToken = default)
    {
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (!isRunning && !isBusy)
            {
                statusText = "Stopped";
                return;
            }

            isBusy = true;
            statusText = "Stopping";
            lastError = null;

            logger.LogTrace("Stopping TibberBackgroundService");

            try
            {
                await StopListenerAsync(cancellationToken);
                isRunning = false;
                statusText = "Stopped";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                statusText = "Stopped";
                logger.LogInformation("TibberBackgroundService stop was canceled");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                logger.LogError(ex, "TibberBackgroundService stop failed");
            }
        }
        finally
        {
            isBusy = false;
            lifecycleLock.Release();
        }
    }

    public async Task ProcessRealTimeMeasurementAsync(RealTimeMeasurement value)
    {
        try
        {
            var tibberRealTimeMeasurement = new TibberRealTimeMeasurement(value);
            UpsertRealTimeMeasurement(tibberRealTimeMeasurement);
            await PersistRealTimeMeasurementAsync(tibberRealTimeMeasurement);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    public void Dispose()
    {
        lifecycleLock.Dispose();
        TibberApiClient?.Dispose();
    }

    #endregion Public Methods

    #region Private Methods

    private async Task PersistRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await dbContext.TibberRealTimeMeasurements.FindAsync(tibberRealTimeMeasurement.TS);
        if (existing is null)
        {
            dbContext.TibberRealTimeMeasurements.Add(tibberRealTimeMeasurement);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(tibberRealTimeMeasurement);
        }

        await dbContext.SaveChangesAsync();
    }

    private void UpsertRealTimeMeasurement(TibberRealTimeMeasurement tibberRealTimeMeasurement)
    {
        lock (TibberRealTimeMeasurement._syncRoot)
        {
            var existingIndex = TibberRealTimeMeasurement.ToList().FindIndex(x => x.TS == tibberRealTimeMeasurement.TS);
            if (existingIndex >= 0)
            {
                TibberRealTimeMeasurement[existingIndex] = tibberRealTimeMeasurement;
            }
            else
            {
                TibberRealTimeMeasurement.Add(tibberRealTimeMeasurement);
            }
        }
    }

    public async Task TibberSavePricesAsync(IList<TibberPrice> prices)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        lock (TibberPrices._syncRoot)
        {
            foreach (var price in prices)
            {
                var existing = TibberPrices.FirstOrDefault(x => x.StartsAt == price.StartsAt);
                if (existing is null)
                {
                    TibberPrices.Add(price);
                    dbContext.TibberPrices.Add(price);
                }
                else
                {
                    dbContext.Entry(existing).CurrentValues.SetValues(price);
                }
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task StartServiceAsync(CancellationToken cancellationToken, bool respectConfiguration)
    {
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (isRunning)
            {
                statusText = "Running";
                return;
            }

            if (respectConfiguration && !configuration.GetSection("BackgroundServices").GetValue("TibberBackgroundService", true))
            {
                statusText = "Disabled by configuration";
                return;
            }

            var token = configuration["ApiSettings:TibberApiToken"];
            if (string.IsNullOrWhiteSpace(token) || token.Contains("your-api-token", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Missing Tibber API token";
                logger.LogWarning("Tibber API token is missing or placeholder. Tibber background service will not start.");
                return;
            }

            isBusy = true;
            statusText = "Starting";
            lastError = null;

            try
            {
                logger.LogTrace("Starting TibberBackgroundService");
                await StartListener(cancellationToken);

                if (TibberHomeId.HasValue)
                {
                    isRunning = true;
                    statusText = "Running";
                }
                else
                {
                    statusText = "Stopped";
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                statusText = "Stopped";
                logger.LogInformation("TibberBackgroundService startup was canceled");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                logger.LogError(ex, "TibberBackgroundService startup failed");
            }
        }
        finally
        {
            isBusy = false;
            lifecycleLock.Release();
        }
    }

    #endregion Private Methods
}
