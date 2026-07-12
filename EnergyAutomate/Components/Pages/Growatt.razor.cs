using BlazorBootstrap;
using EnergyAutomate.Definitions;
using EnergyAutomate.Emulator;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using static Python.Runtime.TypeSpec;

namespace EnergyAutomate.Components.Pages
{
    public partial class Growatt : IDisposable
    {
        private readonly IEnumerable<TickMark> ApiPowerTickList = EnergyAutomateService.GenerateTickTickMarks(0, 900, 50);

        private LineChart deviceChart = default!;
        private ChartData deviceData = default!;
        private bool isDeviceChartInitialized;

        #region Properties

        [Inject]
        public required ApplicationDbContext ApplicationDbContext { get; set; }

        [Inject]
        public required NavigationManager NavigationManager { get; set; }

        [Inject]
        public required ILogger<Growatt> Logger { get; set; }

        [Inject]
        public required PythonWrapper PythonWrapper { get; set; }

        private int SmartPowerValue { get; set; } = 500;
        private int DefaultPowerValue { get; set; } = 250;

        #endregion Properties

        protected override void OnInitialized()
        {
            ApiService.TibberRealTimeMeasurementRegisterOnCollectionChanged(this, RealTimeMeasurement_CollectionChanged);
            ApiService.StateHasChanged += ApiService_StateHasChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RenderDeviceChartAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public void Dispose()
        {
            ApiService.TibberRealTimeMeasurementUnRegisterOnCollectionChanged(this);
            ApiService.StateHasChanged -= ApiService_StateHasChanged;
        }

        private async void ApiService_StateHasChanged(object? sender, EventArgs e)
        {
            await InvokeAsync(StateHasChanged);
        }

        private void GetDeviceData()
        {
            var dataSource = ApiService.TibberListRealTimeMeasurement().OrderByDescending(x => x.TS).Take(61).Reverse().ToList();

            List<double?>? GetDeviceData(string? deviceSn, string propertyName)
            {
                switch (propertyName)
                {
                    case "Requested":
                        return dataSource.Select(x => x.PowerValueNewDeviceSn == deviceSn ? (double?)x.PowerValueNewRequested : null).ToList();
                    default:
                        return new List<double?>();
                }
            }

            deviceData = new ChartData
            {
                Labels = dataSource.Select((x, index) => index % 5 == 0 ? x.TS.ToLocalTime().TimeOfDay.ToString() : string.Empty).ToList(),
                Datasets = new List<IChartDataset>()
                {
                    new LineChartDataset()
                    {
                        Label = "Total Commited",
                        Data = dataSource.Select(x => (double?)x.PowerValueTotalCommited).ToList(),
                        BackgroundColor = "rgb(0, 255, 0)",
                        BorderColor = "rgb(0, 255, 0)",
                        BorderWidth = 4,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Order = 14
                    },
                    new LineChartDataset()
                    {
                        Label = "Total Requested",
                        Data = dataSource.Select(x => (double?)x.PowerValueTotalRequested).ToList(),
                        BackgroundColor = "rgb(255, 0, 0)",
                        BorderColor = "rgb(255, 0, 0)",
                        BorderWidth = 4,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Order = 13
                    },
                    new LineChartDataset()
                    {
                        Label = "New Requested",
                        Data = dataSource.Select(x => (double?)x.PowerValueNewRequested).ToList(),
                        BackgroundColor = "rgb(255, 150, 0)",
                        BorderColor = "rgb(255, 150, 0)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Fill = true,
                        Order = 12
                    },
                    new LineChartDataset()
                    {
                        Label = "New Commited",
                        Data = dataSource.Select(x => (double?)x.PowerValueNewCommited).ToList(),
                        BackgroundColor = "rgb(150, 255, 0)",
                        BorderColor = "rgb(150, 255, 0)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Fill = true,
                        Order = 11
                    }
                },
            };

            Dictionary<int, string> ColorSet = new Dictionary<int, string>() { { 1, "rgb(0, 0, 255)" }, { 2, "rgb(0, 150, 255)" } };

            var index = 1;
            foreach (var device in ApiService.GrowattAllNoahDevices())
            {
                deviceData.Datasets.Add(new LineChartDataset()
                {
                    Label = $"Noah({device.DeviceSn}) Requested",
                    Data = GetDeviceData(device.DeviceSn, "Requested"),
                    BackgroundColor = ColorSet[index],
                    BorderColor = ColorSet[index],
                    BorderWidth = 2,
                    HoverBorderWidth = 4,
                    Stepped = true,
                    Order = index
                });
                index++;
            }
        }

        private async Task RenderDeviceChartAsync()
        {
            if (isDeviceChartInitialized || !ApiService.TibberListRealTimeMeasurement().Any())
            {
                return;
            }

            var deviceChartOptions = new LineChartOptions();

            deviceChartOptions.Interaction.Mode = InteractionMode.Index;
            deviceChartOptions.Plugins.Title!.Text = "Device power values";
            deviceChartOptions.Plugins.Title.Display = true;
            deviceChartOptions.Plugins.Title.Font = new ChartFont { Size = 20 };
            deviceChartOptions.Responsive = true;
            deviceChartOptions.Scales.Y = new ChartAxes() { Min = 0, Max = 1000 };
            deviceChartOptions.Scales.X!.Title = new ChartAxesTitle { Text = "Seconds (one minute)", Display = true };
            deviceChartOptions.Scales.Y!.Title = new ChartAxesTitle { Text = "Watt", Display = true };
            deviceChartOptions.MaintainAspectRatio = false;

            GetDeviceData();
            await deviceChart.InitializeAsync(chartData: deviceData, chartOptions: deviceChartOptions);
            isDeviceChartInitialized = true;
        }

        private async void RealTimeMeasurement_CollectionChanged()
        {
            try
            {
                if (!isDeviceChartInitialized)
                {
                    await RenderDeviceChartAsync();
                }

                if (isDeviceChartInitialized && deviceChart != null)
                {
                    GetDeviceData();
                    if (deviceData != null)
                    {
                        await deviceChart.UpdateValuesAsync(deviceData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in device chart update");
            }
        }

        private async Task SetSmartPowerAsync(DeviceList device)
        {
            PythonWrapper.SetSmartPower(device, SmartPowerValue);
        }

        private async Task SetDefaultPowerAsync(DeviceList device)
        {
            PythonWrapper.SetDefaultPower(device, DefaultPowerValue);
        }

        private async Task ClearDeviceNoahTimeSegmentsAsync(DeviceList device)
        {
            // Clear all 9 time segments (types 1-9) by setting enable to 0
            for (int slot = 1; slot <= 9; slot++)
            {
                var query = new DeviceNoahSetTimeSegmentQuery
                {
                    DeviceSn = device.DeviceSn,
                    DeviceType = device.DeviceType,
                    Type = slot.ToString(),
                    Enable = "0",
                    StartTime = "00:00",
                    EndTime = "00:00",
                    Power = "0",
                    Repeat = ""
                };

                PythonWrapper.SetNoahTimeSegment(query);

                Logger.LogInformation(
                    "[TRACE] ClearAllNoahTimeSegments: Cleared time segment slot {Slot}",
                    slot
                );
            }
        }

        private async Task BattPriorityDeviceNoahAsync(DeviceList device)
        {
            var query = new DeviceNoahSetTimeSegmentQuery
            {
                DeviceSn = device.DeviceSn,
                DeviceType = device.DeviceType,
                Type = "1", // Assuming type 1 is for battery priority
                Enable = "0",
                StartTime = "00:00",
                EndTime = "00:00",
                Power = "0",
                Repeat = ""
            };

            PythonWrapper.SetNoahTimeSegment(query);
        }

        private async Task LoadPriorityDeviceNoahAsync(DeviceList device)
        {
            var query = new DeviceNoahSetTimeSegmentQuery
            {
                DeviceSn = device.DeviceSn,
                DeviceType = device.DeviceType,
                Type = "0", // Assuming type 0 is for load priority
                Enable = "0",
                StartTime = "00:00",
                EndTime = "00:00",
                Power = "0",
                Repeat = ""
            };

            PythonWrapper.SetNoahTimeSegment(query);
        }
    }
}
