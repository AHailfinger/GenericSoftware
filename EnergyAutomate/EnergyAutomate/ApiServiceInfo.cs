using EnergyAutomate;
using Growatt.OSS;
using System.Collections.ObjectModel;
using Tibber.Sdk;

public class ApiServiceInfo
{
    public bool AutoMode { get; set; }

    public int AvgPowerLoadSeconds { get; set; } = 20;

    public int ApiLockSeconds { get; set; } = 6;

    public int ApiOffsetAvg { get; set; } = 25;

    public int ApiTotalAvg 
    {
        get
        {
            return apiTotalAvg;
        }
        set
        {
            if (apiTotalAvg != value)
                ApiTotalAvgChanged?.Invoke(this, new EventArgs());
            apiTotalAvg = value;
        }
    }
    private int apiTotalAvg = 0;

    public event EventHandler? ApiTotalAvgChanged;

    public List<DateTime> DataReads { get; set; } = [];

    public List<RealTimeMeasurementExtention> RealTimeMeasurementExtentions { get; set; } = [];
    
    public List<ApiLastValueChange> LastValueChange { get; set; } = [];

    public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
    
    public ObservableCollection<DeviceNoahInfo> DeviceNoahInfo { get; set; } = new ObservableCollection<DeviceNoahInfo>();
    
    public ObservableCollection<DeviceNoahLastData> DeviceNoahLastData { get; set; } = new ObservableCollection<DeviceNoahLastData>();
    
    public ObservableCollection<RealTimeMeasurement> RealTimeMeasurements { get; set; } = new ObservableCollection<RealTimeMeasurement>();
    
    public ObservableCollection<ApiPrice> Prices { get; set; } = new ObservableCollection<ApiPrice>();

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

    public int LastPowerValue(string deviceSn)
    {
        return LastValueChange.Where(x => x.DeviceSn == deviceSn).Count() != 0 ?
            LastValueChange.OrderByDescending(x => x.TS).First(x => x.DeviceSn == deviceSn).Value :
            (int)DeviceNoahLastData.OrderBy(x => x.time).First(x => x.deviceSn == deviceSn).pac;
    }

    public int LastPowerAvgValue()
    {
        if (LastValueChange.Count != 0)
        {
            var latestPacValues = LastValueChange
                .GroupBy(x => x.DeviceSn)
                .Select(g => g.OrderByDescending(x => x.TS).First().Value)
                .ToList();

            return latestPacValues.Any() ? (int)latestPacValues.Average() : 0;
        }
        else
        {
            var latestPacValues = DeviceNoahLastData
                .GroupBy(x => x.deviceSn)
                .Select(g => g.OrderByDescending(x => x.time).First().pac)
                .ToList();

            return latestPacValues.Any() ? (int)latestPacValues.Average() : 0;
        }
    }

}
