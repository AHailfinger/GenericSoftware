using EnergyAutomate;
using Growatt.OSS;
using System.Collections.ObjectModel;
using Tibber.Sdk;

public class ApiServiceInfo
{
    public bool AutoMode { get; set; }

    public bool SettingLoadBalanced { get; set; } = false;

    public int SettingPowerLoadSeconds { get; set; } = 30;

    public int SettingLockSeconds { get; set; } = 600;

    public int SettingOffsetAvg { get; set; } = 75;

    public int SettingToleranceAvg { get; set; } = 50;

    public int SettingMaxPower { get; set; } = 800;

    public int AvgPowerLoad { get; set; }



    public int DeltaPowerValue { get; set; }
    public int DifferencePowerValue { get; set; }
    public int LastPowerValue { get; set; }
    public int NewPowerValue { get; set; }
  
    public List<DateTime> DataReads { get; set; } = [];

    #region Growatt

    #region OutputValueValueChange

    public event EventHandler? AvgOutputValueChanged;

    public void InvokeAvgOutputValueChanged()
    {
        AvgOutputValueChanged?.Invoke(this, new EventArgs());
    }

    public Queue<ApiOutputValueDeviceInfo> GrowattValueChangeQueue = new();

    public ObservableCollection<ApiOutputValueDeviceInfo> OutputValueValueChange { get; set; } = [];

    public ApiOutputValueDeviceInfo? LastOutputValueValueChange => OutputValueValueChange.OrderByDescending(x => x.TS).FirstOrDefault();

    public bool IsLastOutputValueEqual => OutputValueValueChange.All(x => x.Value == OutputValueValueChange.First().Value);

    public int LastOutputValue => OutputValueValueChange.Sum(s => s.Value);

    public int GetLastValuePerDevice(string deviceSn)
    {
        if (!string.IsNullOrWhiteSpace(deviceSn))
        {
            var apiOutputValueDeviceInfo = OutputValueValueChange.FirstOrDefault(x => x.DeviceSn == deviceSn);
            var lastValueChange = apiOutputValueDeviceInfo?.Value ?? 0;
            return lastValueChange;
        }
        return 0;
    }

    #endregion OutputValueValueChange

    #region Device

    public ObservableCollection<Device> Devices { get; set; } = [];
    
    public ObservableCollection<DeviceNoahInfo> DeviceNoahInfo { get; set; } = [];
    
    public ObservableCollection<DeviceNoahLastData> DeviceNoahLastData { get; set; } = [];

    #endregion Device

    public delegate Task RefreshDeviceListHandler(object sender, EventArgs e);
    public event RefreshDeviceListHandler? RefreshDeviceList;

    public delegate Task ClearDeviceNoahTimeSegmentsHandler(object sender, EventArgs e);
    public event ClearDeviceNoahTimeSegmentsHandler? ClearDeviceNoahTimeSegments;

    public delegate Task RefreshNoahsHandler(object sender, EventArgs e);
    public event RefreshNoahsHandler? RefreshNoahs;

    public delegate Task RefreshNoahLastDataHandler(object sender, EventArgs e);
    public event RefreshNoahLastDataHandler? RefreshNoahLastData;

    public async Task InvokeClearDeviceNoahTimeSegments()
    {
        if (ClearDeviceNoahTimeSegments != null)
        {
            await ClearDeviceNoahTimeSegments(this, EventArgs.Empty);
        }
    }

    public async Task InvokeRefreshDeviceList()
    {
        if (RefreshDeviceList != null)
        {
            await RefreshDeviceList(this, EventArgs.Empty);
        }
    }

    public async Task InvokeRefreshNoahs()
    {
        if (RefreshNoahs != null)
        {
            await RefreshNoahs(this, EventArgs.Empty);
        }
    }

    public async Task InvokeRefreshNoahsLastData()
    {
        if (RefreshNoahLastData != null)
        {
            await RefreshNoahLastData(this, EventArgs.Empty);
        }
    }


    #endregion Growatt

    #region Tibber

    public ObservableCollection<RealTimeMeasurementExtention> RealTimeMeasurement { get; set; } = [];
    
    public ObservableCollection<ApiPrice> Prices { get; set; } = [];

    public (double?, double?, List<double?>, List<double?>) GetPriceDatas()
    {
        var priceDates = Prices.GroupBy(x => x.StartsAt.Date).ToList();
        var result = priceDates.OrderByDescending(x => x.Key).Take(2);
        var today = result.OrderBy(x => x.Key).FirstOrDefault()?.Key;
        var tomorrow = result.OrderBy(x => x.Key).LastOrDefault()?.Key;
        var dataToday = today.HasValue ? Prices.Where(x => x.StartsAt.Date == today.Value.Date).OrderBy(x => x.StartsAt).Select(x => (double?)x.Total).ToList() : new List<double?>();
        var dataTomorrow = tomorrow.HasValue ? Prices.Where(x => x.StartsAt.Date == tomorrow.Value.Date).OrderBy(x => x.StartsAt).Select(x => (double?)x.Total).ToList() : new List<double?>();
        var avgToday = dataToday.Any() ? dataToday.Average() : 0;
        var avgTomorrow = dataTomorrow.Any() ? dataTomorrow.Average() : 0;

        return (avgToday, avgTomorrow, dataToday, dataTomorrow);
    }

    public double? GetAvgPriceToday()
    {
        var (avgToday, _, _, _) = GetPriceDatas();
        return avgToday;
    }

    #endregion Tibber
}
