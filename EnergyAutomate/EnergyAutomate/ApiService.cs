using EnergyAutomate;
using EnergyAutomate.Data;
using Growatt.OSS;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
    }

    private DateTime? lastLimitError = default;

    private bool isRealTimeMeasurementRunning = false;
    private bool isGrowattValueChangeQueueRunning = false;
    private int penaltyFrequentlyAccess = 0;

    private async Task CheckGrowattValueChangeQueue()
    {
        if (isGrowattValueChangeQueueRunning)
            return;

        isGrowattValueChangeQueueRunning = true;

        while (ApiServiceInfo.GrowattValueChangeQueue.Count > 0)
        {
            if (ApiServiceInfo.GrowattValueChangeQueue.Count > 5) ApiServiceInfo.GrowattValueChangeQueue.Clear();

            ApiOutputValueDeviceInfo? apiOutputValueDeviceInfo;
            ApiServiceInfo.GrowattValueChangeQueue.TryDequeue(out apiOutputValueDeviceInfo);
            if (apiOutputValueDeviceInfo != null)
            {
                await SetPower(apiOutputValueDeviceInfo);
            }
            await Task.Delay(ApiServiceInfo.SettingLockSeconds + penaltyFrequentlyAccess);
        }

        isGrowattValueChangeQueueRunning = false;
    }

    private async Task SetPower(ApiOutputValueDeviceInfo valueChange)
    {
        try
        {
            if (ApiServiceInfo.SettingLoadBalanced)
            {
                await GrowattApiClient.SetTimeSegmentAsync(new DeviceTimeSegment()
                {
                    DeviceType = valueChange.DeviceType,
                    DeviceSn = valueChange.DeviceSn,
                    Type = "0",
                    Enable = "1",
                    StartTime = "00:00",
                    EndTime = "23:59",
                    Mode = "0",
                    Power = valueChange.Value.ToString()
                });
            }
            else
                await GrowattApiClient.SetPowerAsync(valueChange.DeviceSn, "noah", valueChange.Value);

            var result = ApiServiceInfo.OutputValueValueChange.FirstOrDefault(x => x.DeviceSn == valueChange.DeviceSn);
            if (result != null)
            {
                result.TS = valueChange.TS;
                result.Value = valueChange.Value;
            }
            else
            {
                ApiServiceInfo.OutputValueValueChange.Add(valueChange);
            }

            var item = ApiServiceInfo.RealTimeMeasurement.FirstOrDefault(x => x.TS == valueChange.TS);
            if (item != null)
            {
                if (item.DeviceInfos == null)
                    item.DeviceInfos = [];

                var info = item.DeviceInfos.FirstOrDefault(x => x.DeviceSn == valueChange.DeviceSn);
                if (info != null)
                {
                    info.TS = valueChange.TS;
                    info.Value = valueChange.Value;
                }
                else
                    item.DeviceInfos.Add(valueChange);
            }

            var dbContext = ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var data = dbContext.RealTimeMeasurements.FirstOrDefault(x => x.TS == valueChange.TS);
            if (data != null)
            {
                if (data.DeviceInfos == null)
                    data.DeviceInfos = [];

                var info = data.DeviceInfos.FirstOrDefault(x => x.DeviceSn == valueChange.DeviceSn);
                if (info != null)
                {
                    info.TS = valueChange.TS;
                    info.Value = valueChange.Value;
                }
                else
                    data.DeviceInfos.Add(valueChange);

                await dbContext.SaveChangesAsync();
            }

            Debug.WriteLine($"{valueChange.DeviceSn}, {valueChange.Value}");

            if (penaltyFrequentlyAccess >= 10)
                penaltyFrequentlyAccess -= 10;

            ApiServiceInfo.InvokeAvgOutputValueChanged();
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
        if (ApiServiceInfo.AutoMode)
        {
            if (!isRealTimeMeasurementRunning)
            {
                isRealTimeMeasurementRunning = true;
                await SetNoTimerDeviceNoahs(this, new EventArgs());
            }

            await LoadDeviceData(sender);

            //current Price is over the average price
            var dataToday = ApiServiceInfo.Prices.Where(x => x.StartsAt.Date == DateTime.Now.Date).OrderBy(x => x.StartsAt).Select(x => new { x.StartsAt, x.Total }).ToList();
            var currentPrice = (double?)dataToday.First(p => p.StartsAt.Hour == DateTime.Now.Hour).Total;

            //Average Price for today
            var avgToday = ApiServiceInfo.GetAvgPriceToday();

            // Prüfe, ob der aktuelle Preis über dem Tagesdurchschnitt liegt
            if (currentPrice.HasValue && currentPrice.Value >= avgToday)
            {
                List<ApiOutputValueDeviceInfo> lastOutputValue = [];

                var absAvgPowerLoad = Math.Abs(ApiServiceInfo.AvgPowerLoad);

                var deltaPowerLoad = absAvgPowerLoad switch
                {
                    > 300 => 100,
                    > 250 => 50,
                    > 200 => 25,
                    > 150 => 20,
                    > 100 => 15,
                    > 75 => 10,
                    > 50 => 5,
                    > 25 => 4,
                    > 20 => 3,
                    > 15 => 2,
                    > 10 => 1,
                    _ => 0
                };
                              
                ApiServiceInfo.LastPowerValue = absAvgPowerLoad >= 50 ? (int)(Math.Round(ApiServiceInfo.LastOutputValue / 2 / 5.0) * 5) : (int)ApiServiceInfo.LastOutputValue / 2;                
                ApiServiceInfo.NewPowerValue = ApiServiceInfo.LastPowerValue;

                // Prüfe, ob der aktuelle Preis über dem Tagesdurchschnitt liegt
                if (ApiServiceInfo.AvgPowerLoad > (ApiServiceInfo.SettingToleranceAvg / 2) + ApiServiceInfo.SettingOffsetAvg)
                {
                    ApiServiceInfo.NewPowerValue = ApiServiceInfo.LastPowerValue + deltaPowerLoad;
                }
                else if (ApiServiceInfo.AvgPowerLoad < -(ApiServiceInfo.SettingToleranceAvg / 2) + ApiServiceInfo.SettingOffsetAvg)
                {
                    ApiServiceInfo.NewPowerValue = ApiServiceInfo.LastPowerValue - deltaPowerLoad;
                }

                var powerChanged = ApiServiceInfo.NewPowerValue != ApiServiceInfo.LastPowerValue;

                ApiServiceInfo.NewPowerValue = ApiServiceInfo.NewPowerValue > 450 ? 450 : ApiServiceInfo.NewPowerValue < 0 ? 0 : ApiServiceInfo.NewPowerValue;

                if (ApiServiceInfo.NewPowerValue <= 450 && powerChanged)
                {
                    foreach (var device in ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah"))
                    {
                        var last = ApiServiceInfo.GetLastValuePerDevice(device.DeviceSn);
                        if (last == 0 || ApiServiceInfo.NewPowerValue == 0 || last != ApiServiceInfo.NewPowerValue)
                        {
                            var item = new ApiOutputValueDeviceInfo()
                            {
                                DeviceType = device.DeviceType,
                                DeviceSn = device.DeviceSn,
                                Value = ApiServiceInfo.NewPowerValue,                                
                                TS = value.Timestamp.DateTime
                            };

                            ApiServiceInfo.GrowattValueChangeQueue.Enqueue(item);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"PowerChanged: {powerChanged}, OffSet: {ApiServiceInfo.SettingOffsetAvg}");
                }


                await CheckGrowattValueChangeQueue();
            }
        }
        else
        {
            if (isRealTimeMeasurementRunning)
            {
                await SetNoTimerDeviceNoahs(this, new EventArgs());

                foreach (var device in ApiServiceInfo.Devices.Where(x => x.DeviceType == "noah"))
                {
                    await GrowattApiClient.SetPowerAsync(device.DeviceSn, device.DeviceType, 0);
                    await Task.Delay(1000);
                }
            }
            isRealTimeMeasurementRunning = false;
        }
    }

    private async Task LoadDeviceData(object sender)
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
            if (RTMObserver != null)
                RTMObserver.OnNewRealTimeMeasurement -= RealTimeMeasurement_OnNewRealTimeMeasurement;

            RTMObserver = new RealTimeMeasurementObserver(ServiceProvider);

            RealTimeMeasurementListener = await TibberApiClient.StartRealTimeMeasurementListener(TibberHomeId.Value);
            RealTimeMeasurementListener.Subscribe(RTMObserver);

            RTMObserver.OnNewRealTimeMeasurement += RealTimeMeasurement_OnNewRealTimeMeasurement;
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

        var realTimeMeasurements = await dbContext.RealTimeMeasurements.OrderByDescending(x => x.Timestamp).Take(100).ToListAsync();
        ApiServiceInfo.RealTimeMeasurement.Clear();
        foreach (var measurement in realTimeMeasurements)
        {
            ApiServiceInfo.RealTimeMeasurement.Add(measurement);
        }

        var lastItem = ApiServiceInfo.RealTimeMeasurement.OrderByDescending(x => x.Timestamp).FirstOrDefault();
        if (lastItem != null && lastItem.DeviceInfos != null && lastItem.DeviceInfos.Count == 2)
        {
            ApiServiceInfo.OutputValueValueChange = [.. lastItem.DeviceInfos];
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

                await Task.Delay(1000);
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
