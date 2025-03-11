using BlazorBootstrap;
using Growatt.OSS;
using Mono.TextTemplating;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Linq;
using static ApiServiceInfo;

namespace EnergyAutomate.Components.Pages
{
    public partial class RealTimeMeasurement : IDisposable
    {
        protected override void OnInitialized()
        {
            ApiServiceInfo.RealTimeMeasurements.CollectionChanged += RealTimeMeasurement_CollectionChanged;
            ApiServiceInfo.Prices.CollectionChanged += Price_CollectionChanged;

            ApiServiceInfo.Devices.CollectionChanged += Devices_CollectionChanged;
            ApiServiceInfo.DeviceNoahLastData.CollectionChanged += DeviceNoahLastData_CollectionChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                isRealTimeMeasurementChartInitialized = false;
                await RenderTibberAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private readonly IEnumerable<TickMark> AvgPowerLoadSecondsTickList = new List<TickMark>
        {
            new(){ Label = "5", Value = "5"},
            new(){ Label = "10", Value = "10"},
            new(){ Label = "15", Value = "15"},
            new(){ Label = "20", Value = "20"},
            new(){ Label = "25", Value = "25"},
            new(){ Label = "30", Value = "30"}
        };

        private readonly IEnumerable<TickMark> ApiLockSecondsTickList = new List<TickMark>
        {
            new(){ Label = "5", Value = "5"},
            new(){ Label = "10", Value = "10"},
            new(){ Label = "15", Value = "15"}
        };

        #region Tibber

        private LineChart realTimeMeasurementChart = default!;
        private ChartData realTimeMeasurementData = default!;
        private bool isRealTimeMeasurementChartInitialized;

        private async void RealTimeMeasurement_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (isRealTimeMeasurementChartInitialized && realTimeMeasurementChart != null)
            {
                GetRealTimeMeasurementData();
                if (realTimeMeasurementData != null)
                {
                    await realTimeMeasurementChart.UpdateValuesAsync(realTimeMeasurementData);
                }
            }
        }

        private LineChart priceChart = default!;
        private ChartData priceData = default!;
        private bool isPriceChartInitialized;

        private async void Price_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (isPriceChartInitialized && priceChart != null)
            {
                GetRealTimeMeasurementData();
                if (priceData != null)
                {
                    await priceChart.UpdateValuesAsync(priceData);
                }
            }
        }

        private void GetRealTimeMeasurementData()
        {
            var count = ApiServiceInfo.RealTimeMeasurements.Count;
            var dataSource = ApiServiceInfo.RealTimeMeasurements.OrderByDescending(x => x.Timestamp).Take(61).Reverse().ToList();

            // Berechne den Durchschnittsverbrauch für jeden Datenpunkt basierend auf den letzten 30 Sekunden
            var avgPowerLoad = new List<double?>();
            foreach (var dataPoint in dataSource)
            {
                var lastSecondsData = dataSource.Where(x => x.Timestamp >= dataPoint.Timestamp.AddSeconds(ApiServiceInfo.AvgPowerLoadSeconds *(-1)) && x.Timestamp <= dataPoint.Timestamp).ToList();
                var avgPower = lastSecondsData.Any() ? lastSecondsData.Average(x => (double?)x.Power) : 0;
                var avgPowerProduction = lastSecondsData.Any() ? lastSecondsData.Average(x => (double?)x.PowerProduction) : 0;
                avgPowerLoad.Add((avgPower + avgPowerProduction) / 2);
            }

            realTimeMeasurementData = new ChartData
            {
                Labels = dataSource.Select((x, index) => index % 5 == 0 ? x.Timestamp.TimeOfDay.ToString() : string.Empty).ToList(),
                Datasets = new List<IChartDataset>()
                {
                    new LineChartDataset()
                    {
                        Label = "Consumtion",
                        Data = dataSource.Select(x => (double?)x.Power).ToList(),
                        BackgroundColor = "rgb(88, 80, 141)",
                        BorderColor = "rgb(88, 80, 141)",
                        BorderWidth = 2,
                        HoverBorderWidth = 4,
                        Fill = true,
                        Stepped = true,
                        Order = 3
                    },
                    new LineChartDataset()
                    {
                        Label = "Production",
                        Data = dataSource.Select(x => (double?)x.PowerProduction).Select(s => s *(-1)).ToList(),
                        BackgroundColor = "rgb(255, 166, 0)",
                        BorderColor = "rgb(255, 166, 0)",
                        BorderWidth = 2,
                        HoverBorderWidth = 4,
                        Fill = true,
                        Stepped = true,
                        Order = 2
                    },
                    new LineChartDataset()
                    {
                        Label = "AvgPowerLoad",
                        Data = avgPowerLoad,
                        BackgroundColor = "rgb(0, 0, 0)",
                        BorderColor = "rgb(0, 0, 0)",
                        BorderWidth = 1,
                        PointRadius = new List<double>() { 0 },                        
                        Order = 1
                    }
               }
            };
        }

        private void GetPriceData()
        {
            double? avgToday;
            double? avgTomorrow;
            List<double?> dataToday;
            List<double?> dataTomorrow;

            (avgToday, avgTomorrow, dataToday, dataTomorrow) = ApiServiceInfo.GetPriceDatas();

            List<string> months = new List<string> { "Today", "Tomorrow" };
            var dataPoints = dataToday.Concat(dataTomorrow).ToList();

            var dataAvgTodayPoints = dataToday.Select(point => point > avgToday ? avgToday : point).ToList();
            var dataAvgTomorrowPoints = dataTomorrow.Select(point => point > avgTomorrow ? avgTomorrow : point).ToList();

            var dataAvgPoints = dataAvgTodayPoints.Concat(dataAvgTomorrowPoints).ToList();

            priceData = new ChartData
            {
                Labels = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24" },
                Datasets = new List<IChartDataset>()
                {
                    new LineChartDataset()
                    {
                        Label = "Price",
                        Data = dataPoints,
                        BackgroundColor = "rgb(255, 166, 0)",
                        BorderColor = "rgb(255, 166, 0)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 0.0 },
                        HoverBorderWidth = 4,
                        Fill = true,
                        Stepped = true,
                        Order = 2 // Hintergrund
                    },
                    new LineChartDataset()
                    {
                        Label = "Avg",
                        Data = dataAvgPoints,
                        BackgroundColor = "rgb(88, 80, 141)",
                        BorderColor = "rgb(88, 80, 141)",
                        BorderWidth = 0,
                        PointRadius = new List<double>() { 0.0 },
                        HoverBorderWidth = 4,
                        Fill = true,
                        Stepped = true,
                        Order = 1 // Hintergrund
                    },
                }
            };

            var count = ApiServiceInfo.Prices.Count;
            var dataSource = ApiServiceInfo.Prices.OrderByDescending(x => x.StartsAt).ToList();
        }

        private async Task RenderTibberAsync()
        {
            var realTimeMeasurementChartOptions = new LineChartOptions();

            realTimeMeasurementChartOptions.Interaction.Mode = InteractionMode.Index;
            realTimeMeasurementChartOptions.Plugins.Title!.Text = "Tibber power consumption";
            realTimeMeasurementChartOptions.Plugins.Title.Display = true;
            realTimeMeasurementChartOptions.Plugins.Title.Font = new ChartFont { Size = 20 };
            realTimeMeasurementChartOptions.Responsive = true;
            realTimeMeasurementChartOptions.Scales.X = new ChartAxes() { Min = -800, Max = 8000 };
            realTimeMeasurementChartOptions.Scales.X!.Title = new ChartAxesTitle { Text = "Seconds (one minute)", Display = true };
            realTimeMeasurementChartOptions.Scales.Y!.Title = new ChartAxesTitle { Text = "Watt", Display = true };
            realTimeMeasurementChartOptions.MaintainAspectRatio = false;
            GetRealTimeMeasurementData();
            await realTimeMeasurementChart.InitializeAsync(chartData: realTimeMeasurementData, chartOptions: realTimeMeasurementChartOptions);
            isRealTimeMeasurementChartInitialized = true;

            var priceChartOptions = new LineChartOptions { };
            priceChartOptions.Interaction.Mode = InteractionMode.Index;
            priceChartOptions.Plugins.Title!.Text = "Tibber price forecast";
            priceChartOptions.Plugins.Title.Display = true;
            priceChartOptions.Plugins.Title.Font = new ChartFont { Size = 20 };
            priceChartOptions.Responsive = true;
            priceChartOptions.Scales.X!.Title = new ChartAxesTitle { Text = "Today / Tomorrow", Display = true };
            priceChartOptions.Scales.Y!.Title = new ChartAxesTitle { Text = "Euro", Display = true };
            priceChartOptions.MaintainAspectRatio = false;

            GetPriceData();
            await priceChart.InitializeAsync(chartData: priceData, chartOptions: priceChartOptions);
            isPriceChartInitialized = true;

        }

        #endregion Tibber

        #region Growatt

        Tabs tabsRef = default!;

        private async void Devices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            await InvokeAsync(StateHasChanged);
        }

        private async void DeviceNoahLastData_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            await InvokeAsync(StateHasChanged);
        }

        private async Task RefreshDeviceList()
        {
            await ApiServiceInfo.InvokeRefreshDeviceList();
        }

        private async Task ClearDeviceNoahTimeSegments()
        {
            await ApiServiceInfo.InvokeClearDeviceNoahTimeSegments();
        }

        private async Task RefreshNoahs()
        {
            await ApiServiceInfo.InvokeRefreshNoahs();
        }

        private async Task RefreshNoahLastData()
        {
            await ApiServiceInfo.InvokeRefreshNoahsLastData();
        }

        #endregion Growatt

        public void Dispose()
        {
            ApiServiceInfo.RealTimeMeasurements.CollectionChanged -= RealTimeMeasurement_CollectionChanged;
            ApiServiceInfo.Prices.CollectionChanged -= Price_CollectionChanged;
            ApiServiceInfo.Devices.CollectionChanged -= Devices_CollectionChanged;
        }

    }
}