using BlazorBootstrap;
using EnergyAutomate;
using EnergyAutomate.Data;
using Growatt.OSS;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Claims;
using Tibber.Sdk;

public partial class ApiService : IDisposable
{
    private IServiceProvider ServiceProvider { get; set; }

    public ApiService(IServiceProvider serviceProvider, GrowattApiClient openApiClient, TibberApiClient tibberApiClient, ApiServiceInfo apiServiceInfo)
    {
        ServiceProvider = serviceProvider;
        GrowattApiClient = openApiClient;
        TibberApiClient = tibberApiClient;
        ApiServiceInfo = apiServiceInfo;

        ApiServiceInfo.RefreshDeviceList += RefreshDeviceList;
        ApiServiceInfo.RefreshNoahs += RefreshDeviceNoahs;
        ApiServiceInfo.RefreshNoahLastData += RefreshDeviceNoahLastData;
        ApiServiceInfo.ClearDeviceNoahTimeSegments += SetNoTimerDeviceNoahs;

        RTMObserver = new RealTimeMeasurementObserver(serviceProvider);
        RTMObserver.OnNewRealTimeMeasurement += RealTimeMeasurement_OnNewRealTimeMeasurement;
    }

    private DateTime? lastLimitError = default;

    private bool isRunning = false;
    private int penaltyFrequentlyAccess = 0;

    private async Task CheckGrowattValueChangeQueue()
    {
        if (isRunning)
            return;

        isRunning = true;

        while (ApiServiceInfo.GrowattValueChangeQueue.Count > 0)
        {
            ApiLastValueChange? apiLastValueChange;
            ApiServiceInfo.GrowattValueChangeQueue.TryDequeue(out apiLastValueChange);
            if (apiLastValueChange != null)
            {
                await Send(apiLastValueChange);
            }
            await Task.Delay(ApiServiceInfo.SettingLockSeconds + penaltyFrequentlyAccess);
        }

        isRunning = false;
    }

    private async Task Send(ApiLastValueChange apiLastValueChange)
    {
        try
        {
            if (ApiServiceInfo.LastOutputValue(apiLastValueChange.DeviceSn) != apiLastValueChange.Value)
            {
                await SetPowerDeviceNoahAsync(apiLastValueChange.DeviceSn, apiLastValueChange.Value);
                ApiServiceInfo.OutputValueValueChange.Add(apiLastValueChange);
                Debug.WriteLine($"{apiLastValueChange.DeviceSn}, {apiLastValueChange.Value}");
                ApiServiceInfo.AvgOutputValue = ApiServiceInfo.AvgLastOutputValue();
            } 
            if(penaltyFrequentlyAccess >= 10)
                penaltyFrequentlyAccess -= 10;
        }
        catch (Exception ex)
        {
            ApiServiceInfo.GrowattValueChangeQueue.Clear();
            penaltyFrequentlyAccess += 50;

            Debug.WriteLine($"Growatt Api Error: {ex.Message}");
            Debug.WriteLine($"SettingLockSeconds: {ApiServiceInfo.SettingLockSeconds}, Penalty: {penaltyFrequentlyAccess}");
        }
    }

    private async Task RealTimeMeasurement_OnNewRealTimeMeasurement(object sender, RealTimeMeasurement value)
    {
        var dataSource = ApiServiceInfo.RealTimeMeasurement.OrderByDescending(x => x.Timestamp).Take(61).Reverse().ToList();

        var lastSecondsData = dataSource.Where(x => x.Timestamp >= value.Timestamp.AddSeconds(ApiServiceInfo.SettingPowerLoadSeconds * (-1)) && x.Timestamp <= value.Timestamp).ToList();
        var avgPower = lastSecondsData.Any() ? lastSecondsData.Average(x => (double?)x.Power) : 0;
        var avgPowerProduction = lastSecondsData.Any() ? lastSecondsData.Average(x => (double?)x.PowerProduction) : 0;

        ApiServiceInfo.AvgPowerValue = (int)(avgPower - avgPowerProduction);

        if (ApiServiceInfo.AutoMode)
        {
            var count = ApiServiceInfo.DataReads.Where(x => x > DateTime.Now.AddMinutes(-5)).Count();
            if
            (
                (!lastLimitError.HasValue || lastLimitError.Value.AddMinutes(5) < DateTime.Now) &&
                count < 100
            )
            {
                ApiServiceInfo.DataReads.Add(DateTime.Now);
                try
                {
                    await RefreshDeviceNoahLastData(sender, new EventArgs());
                    lastLimitError = null;
                }
                catch (Exception)
                {
                    lastLimitError = DateTime.Now;
                }

            }

            //Average Price for today
            var avgToday = ApiServiceInfo.GetAvgPriceToday();

            //current Price is over the average price
            var dataToday = ApiServiceInfo.Prices.Where(x => x.StartsAt.Date == DateTime.Now.Date).OrderBy(x => x.StartsAt).Select(x => new { x.StartsAt, x.Total }).ToList();
            var currentPrice = (double?)dataToday.First(p => p.StartsAt.Hour == DateTime.Now.Hour).Total;

            // Berechne den durchschnittlichen Verbrauch in den letzten 30 Sekunden
            var lastSecondsConsuption = ApiServiceInfo.RealTimeMeasurement
                .Where(x => x.Timestamp >= DateTimeOffset.Now.AddSeconds(ApiServiceInfo.SettingPowerLoadSeconds * (-1)))
                .Select(x => x.Power)
                .ToList();

            var averageConsumptionLastSeconds = lastSecondsConsuption.Any() ? lastSecondsConsuption.Average() : 0;

            // Berechne den durchschnittlichen Verbrauch in den letzten 30 Sekunden
            var lastSecondsProduction = ApiServiceInfo.RealTimeMeasurement
                .Where(x => x.Timestamp >= DateTimeOffset.Now.AddSeconds(ApiServiceInfo.SettingPowerLoadSeconds * (-1)))
                .Select(x => x.PowerProduction)
                .ToList();

            var averageProductionLastSeconds = lastSecondsProduction.Any() ? (int?)(lastSecondsProduction.Average()) ?? 0 : 0;
            var lastPowerAvgValue = ApiServiceInfo.AvgLastOutputValue();

            ApiServiceInfo.AvgPowerLoad = (int)(averageConsumptionLastSeconds - averageProductionLastSeconds);

            // Prüfe, ob der aktuelle Preis über dem Tagesdurchschnitt liegt
            if (currentPrice.HasValue && currentPrice.Value >= avgToday)
            {
                int sumBatteryLoad = 0;
                foreach (var device in ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah"))
                {
                    sumBatteryLoad = sumBatteryLoad + ApiServiceInfo.OutputValueValueChange.OrderByDescending(x => x.TS).FirstOrDefault()?.Value ?? 0;
                }
                var avgBatteryLoad = (int)(sumBatteryLoad / ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Count());

                var countNoah = ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Count();

                var lastPowerValue = (int)(Math.Round(ApiServiceInfo.AvgLastOutputValue() / 5.0) * 5);

                foreach (var device in ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah"))
                {
                    int newPowerValue = lastPowerValue;

                    ApiServiceInfo.AvgPowerLoadFactor = Math.Abs(ApiServiceInfo.AvgPowerLoad) switch
                    {
                        > 200 => 5,
                        > 150 => 4,
                        > 100 => 3,
                        > 50 => 2,
                        _ => 1
                    };

                    // Prüfe, ob der aktuelle Preis über dem Tagesdurchschnitt liegt
                    if (ApiServiceInfo.AvgPowerLoad > ApiServiceInfo.SettingOffsetAvg)
                    {
                        newPowerValue = lastPowerValue + 5 * ApiServiceInfo.AvgPowerLoadFactor;
                    }
                    else if (ApiServiceInfo.AvgPowerLoad < ApiServiceInfo.SettingOffsetAvg)
                    {
                        newPowerValue = lastPowerValue - 5 * ApiServiceInfo.AvgPowerLoadFactor;
                    }

                    var abs = Math.Abs(ApiServiceInfo.AvgPowerLoad - ApiServiceInfo.SettingOffsetAvg);

                    var powerChanged = newPowerValue != lastPowerValue;

                    newPowerValue = newPowerValue > 450 ? 450 : (int)(Math.Round(newPowerValue / 5.0) * 5);

                    if (newPowerValue <= 450 && powerChanged && abs > ApiServiceInfo.SettingOffsetAvg * 2)
                    {
                        ApiServiceInfo.GrowattValueChangeQueue.Enqueue(new ApiLastValueChange()
                        {
                            DeviceSn = device.DeviceSn,
                            TS = DateTime.Now,
                            Value = newPowerValue
                        });
                    }
                    else
                        Debug.WriteLine($"DeviceSn: {device.DeviceSn} PowerChanged: {powerChanged}, Abs: {abs} > {ApiServiceInfo.SettingOffsetAvg * 2}");
                }

                await CheckGrowattValueChangeQueue();
            }
        }
        else
        {
            foreach (var device in ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah"))
            {
                if (ApiServiceInfo.LastOutputValue(device.DeviceSn) > 0)
                {
                    ApiServiceInfo.GrowattValueChangeQueue.Enqueue(new ApiLastValueChange()
                    {
                        DeviceSn = device.DeviceSn,
                        TS = DateTime.Now,
                        Value = 0
                    });
                }
            }
        }
    }

    private ApplicationDbContext GetDbContext()
    {
        return ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    private RealTimeMeasurementObserver RTMObserver { get; set; }

    private IObservable<RealTimeMeasurement> RealTimeMeasurementListener { get; set; }

    public ApiServiceInfo ApiServiceInfo { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await GetTomorrowPrices();

        await StartRealTimeMeasurementListener();

        await LoadApiServiceInfoFromDatabase();
    }

    public async Task StartRealTimeMeasurementListener()
    {
        if (TibberHomeId.HasValue)
        {
            RealTimeMeasurementListener = await TibberApiClient.StartRealTimeMeasurementListener(TibberHomeId.Value);
            RealTimeMeasurementListener.Subscribe(RTMObserver);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (TibberHomeId.HasValue)
            await TibberApiClient.StopRealTimeMeasurementListener(TibberHomeId.Value);
    }

    public async Task LoadApiServiceInfoFromDatabase()
    {
        var dbContext = GetDbContext();

        var devices = await dbContext.Devices.ToListAsync();
        ApiServiceInfo.Devices.Clear();
        foreach (var device in devices)
        {
            ApiServiceInfo.Devices.Add(device);
        }

        var deviceNoahInfoList = await dbContext.DeviceNoahInfo.ToListAsync();
        ApiServiceInfo.DeviceNoahInfo.Clear();
        foreach (var info in deviceNoahInfoList)
        {
            ApiServiceInfo.DeviceNoahInfo.Add(info);
        }

        var deviceNoahLastDataList = await dbContext.DeviceNoahLastData.ToListAsync();
        ApiServiceInfo.DeviceNoahLastData.Clear();
        foreach (var lastData in deviceNoahLastDataList)
        {
            ApiServiceInfo.DeviceNoahLastData.Add(lastData);
        }

        var realTimeMeasurements = await dbContext.RealTimeMeasurements.ToListAsync();
        ApiServiceInfo.RealTimeMeasurement.Clear();
        foreach (var measurement in realTimeMeasurements)
        {
            ApiServiceInfo.RealTimeMeasurement.Add(measurement);
        }
    }

    #region Tibber

    public TibberApiClient TibberApiClient { get; set; }

    private Guid? TibberHomeId { get; set; }

    public async Task GetDataFromTibber()
    {
        var basicData = await TibberApiClient.GetBasicData();
        TibberHomeId = basicData.Data.Viewer.Homes.FirstOrDefault()?.Id;

        if (TibberHomeId.HasValue)
        {
            var consumption = await TibberApiClient.GetHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly);

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
            var result = await TibberApiClient.Query(customQuery);

            var query = new TibberQueryBuilder().WithHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly, 12).Build();
            var response = await TibberApiClient.Query(query);
        }
    }

    public async Task GetTomorrowPrices()
    {
        var basicData = await TibberApiClient.GetBasicData();
        TibberHomeId = basicData.Data.Viewer.Homes.FirstOrDefault()?.Id;

        if (TibberHomeId.HasValue)
        {
            // Erstellung der benutzerdefinierten Abfrage mit dem TibberQueryBuilder
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
                                    .WithToday(
                                        new PriceQueryBuilder()
                                        .WithAllScalarFields()
                                    )
                                    .WithTomorrow(
                                        new PriceQueryBuilder()
                                        .WithAllScalarFields()
                                    )
                                )
                        ),
                        TibberHomeId
                    )
                );

            // Abfrage ausführen
            var customQuery = customQueryBuilder.Build(); // Erzeugt den GraphQL-Abfragetext
            var result = await TibberApiClient.Query(customQuery);

            await SavePrices(result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Tomorrow);
            await SavePrices(result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Today);
        }
    }

    private async Task SavePrices(ICollection<Price> prices)
    {
        var dbContext = GetDbContext();

        // Verarbeitung der Ergebnisse

        foreach (var item in prices)
        {
            var price = new ApiPrice()
            {
                StartsAt = DateTime.Parse(item.StartsAt),
                Total = item.Total,
                Level = item.Level
            };

            Console.WriteLine($"Zeit: {price.StartsAt}, Preis: {price.Total} EUR/kWh");

            //Prüfe ob es denn eintrag schon gibt und falls ja mach ein update

            ApiServiceInfo.Prices.Add(price);

            // Prüfe, ob der Datensatz bereits existiert
            var existingDevice = await dbContext.Prices.FindAsync(price.StartsAt);
            if (existingDevice != null)
            {
                // Aktualisiere den bestehenden Datensatz
                dbContext.Entry(existingDevice).CurrentValues.SetValues(price);
            }
            else
            {
                // Füge den neuen Datensatz hinzu
                dbContext.Prices.Add(price);
            }
        }
        await dbContext.SaveChangesAsync(); // Änderungen speichern
    }

    #endregion Tibber

    #region Growatt

    public GrowattApiClient GrowattApiClient { get; set; }

    private async Task RefreshDeviceList(object sender, EventArgs e)
    {
        await GetDevice();
        await RefreshDeviceNoahs(sender, e);
    }

    private async Task RefreshDeviceNoahs(object sender, EventArgs e)
    {
        var deviceSnList = string.Join(",", ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).ToList());
        await GetDeviceNoahInfo("noah", deviceSnList);
        await GetDeviceNoahLastData("noah", deviceSnList);
    }

    private async Task RefreshDeviceNoahLastData(object sender, EventArgs e)
    {
        var deviceSnList = string.Join(",", ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).ToList());
        await GetDeviceNoahLastData("noah", deviceSnList);
    }

    private async Task GetDevice()
    {
        var dbContext = GetDbContext();

        var devices = await GrowattApiClient.GetDeviceListAsync();

        foreach (var device in devices)
        {
            //Prüfe ob es denn eintrag schon gibt und falls ja mach ein update

            ApiServiceInfo.Devices.Add(device);

            // Prüfe, ob der Datensatz bereits existiert
            var existingDevice = await dbContext.Devices.FindAsync(device.DeviceSn);
            if (existingDevice != null)
            {
                // Aktualisiere den bestehenden Datensatz
                dbContext.Entry(existingDevice).CurrentValues.SetValues(device);
            }
            else
            {
                // Füge den neuen Datensatz hinzu
                dbContext.Devices.Add(device);
            }
        }
        await dbContext.SaveChangesAsync(); // Änderungen speichern
    }

    private async Task GetDeviceNoahInfo(string deviceType, string deviceSn)
    {
        var dbContext = GetDbContext();

        var deviceInfoList = await GrowattApiClient.GetDeviceInfoAsync(deviceType, deviceSn);

        foreach (var item in deviceInfoList)
        {
            ApiServiceInfo.DeviceNoahInfo.Add(item);

            // Prüfe, ob der Datensatz bereits existiert
            var existingDeviceNoahInfo = await dbContext.DeviceNoahInfo.FindAsync(item.DeviceSn);
            if (existingDeviceNoahInfo != null)
            {
                // Aktualisiere den bestehenden Datensatz
                dbContext.Entry(existingDeviceNoahInfo).CurrentValues.SetValues(item);
            }
            else
            {
                // Füge den neuen Datensatz hinzu
                dbContext.DeviceNoahInfo.Add(item);
            }
        }

        await dbContext.SaveChangesAsync(); // Änderungen speichern
    }

    private async Task GetDeviceNoahLastData(string deviceType, string deviceSn)
    {
        var dbContext = GetDbContext();

        var deviceLastDataList = await GrowattApiClient.GetDeviceLastDataAsync(deviceType, deviceSn);

        foreach (var item in deviceLastDataList)
        {
            ApiServiceInfo.DeviceNoahLastData.Add(item);

            // Prüfe, ob der Datensatz bereits existiert
            var existingDeviceNoahLastData = await dbContext.DeviceNoahLastData.FindAsync(item.deviceSn, item.time);
            if (existingDeviceNoahLastData != null)
            {
                // Aktualisiere den bestehenden Datensatz
                dbContext.Entry(existingDeviceNoahLastData).CurrentValues.SetValues(item);
            }
            else
            {
                // Füge den neuen Datensatz hinzu
                dbContext.DeviceNoahLastData.Add(item);
            }
        }

        await dbContext.SaveChangesAsync(); // Änderungen speichern

        //fülle hier die TimeSegment
    }

    #region Configs

    private async Task SetNoTimerDeviceNoahs(object sender, EventArgs e)
    {
        var deviceSnList = ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).ToList();

        foreach (var deviceSn in deviceSnList)
        {
            for (int i = 1; i <= 9; i++)
            {

                // Setzt im growat client die detaildata
                var request = new DeviceTimeSegment
                {
                    DeviceSn = deviceSn,
                    DeviceType = "noah",
                    Type = i.ToString(),
                    StartTime = "0:0",
                    EndTime = "0:0",
                    Mode = "0",
                    Power = "0",
                    Enable = "0"
                };

                await GrowattApiClient.SetTimeSegmentAsync(request);

                await Task.Delay(100);
            }
        }
    }

    private async Task SetPowerDeviceNoahsAsync(int value)
    {
        var deviceSnList = ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).ToList();

        foreach (var deviceSn in deviceSnList)
        {
            await GrowattApiClient.SetPowerAsync(deviceSn, "noah", value);
        }
    }

    private async Task SetPowerDeviceNoahAsync(string deviceSn, int value)
    {
        await GrowattApiClient.SetPowerAsync(deviceSn, "noah", value);
    }

    #endregion

    #endregion Growatt

    public void Dispose()
    {
        // Dispose-Logik hier
    }
}
