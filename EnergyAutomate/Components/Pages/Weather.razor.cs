using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using OpenMeteo;
using System.Diagnostics.CodeAnalysis;

namespace EnergyAutomate.Components.Pages
{
    public partial class Weather
    {
        [Inject]
        [AllowNull]
        private ILogger<Weather> Logger { get; set; }

        private LineChart radiationChart = default!;
        private ChartData radiationData = default!;
        private LineChart cloudcoverChart = default!;
        private ChartData cloudcoverData = default!;

        private bool isWeatherChartInitialized;

        #region Properties

        public WeatherForecast? WeatherForecast { get; set; }

        #endregion Properties

        #region Protected Methods

        protected override async Task OnInitializedAsync()
        {
            WeatherForecast = await ApiService.CurrentState.GetWeatherForecastAsync();

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                isWeatherChartInitialized = false;
                await RenderChartsAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion Protected Methods

        private async Task GetWeatherData()
        {
            var weatherForecast = await ApiService.CurrentState.GetWeatherForecastAsync();
            var labelValues = weatherForecast?.Hourly?.Time?.Select(s => DateTime.Parse(s).ToString("HH:mm")).ToList();
            var directRadiationInstant = weatherForecast?.Hourly?.Direct_radiation_instant?.Select(s => (double?)s).ToList();
            var chargeWindow = weatherForecast?.Hourly?.Direct_radiation_instant?.Select(s => (double?)s).ToList();
            var hourlyIndex = weatherForecast?.Hourly?.Time?.Select(s => DateTime.Parse(s).ToUniversalTime()).ToArray();

            if (chargeWindow != null && hourlyIndex != null)
            {

                for (int i = 0; i < chargeWindow.Count; i++)
                {
                    if
                    (
                        !(
                        hourlyIndex[i].Hour > ApiService.CurrentState.BatteryChargeStart.Hour ||
                        hourlyIndex[i].Hour < ApiService.CurrentState.BatteryChargeEnd.Hour
                        )
                    )
                    {
                        chargeWindow[i] = null;
                    }
                }
            }

            var cloudcoverLow = weatherForecast?.Hourly?.Cloudcover_low?.Select(s => (double?)s).ToList();
            var cloudcoverMid = weatherForecast?.Hourly?.Cloudcover_mid?.Select(s => (double?)s).ToList();
            var cloudcoverHigh = weatherForecast?.Hourly?.Cloudcover_high?.Select(s => (double?)s).ToList();

            radiationData = new ChartData
            {
                Labels = labelValues,
                Datasets = new List<IChartDataset>()
                {
                    new LineChartDataset()
                    {

                        Label = "charge_window",
                        Data = chargeWindow,
                        BackgroundColor = "rgba(0, 255, 0, 0.5)",
                        BorderColor = "rgb(0, 255, 0)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Fill = true,
                        Order = 2
                    },
                    new LineChartDataset()
                    {

                        Label = "direct_radiation_instant",
                        Data = directRadiationInstant,
                        BackgroundColor = "rgb(255, 0, 0)",
                        BorderColor = "rgb(255, 0, 0)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 0 },
                        Stepped = true,
                        Order = 1
                    }
                },
            };

            cloudcoverData = new ChartData
            {
                Labels = labelValues,
                Datasets = new List<IChartDataset>()
                {
                    new LineChartDataset()
                    {
                        Label = "cloudcover_low",
                        Data = cloudcoverLow,
                        BackgroundColor = "rgba(173,216,230, 0.5)",
                        BorderColor = "rgb(173,216,230)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 2 },
                        Stepped = true,
                        Fill = true,
                        Order = 1
                    },
                    new LineChartDataset()
                    {
                        Label = "cloudcover_mid",
                        Data = cloudcoverMid,
                        BackgroundColor = "rgba(0, 0, 255, 0.5)",
                        BorderColor = "rgb(0, 0, 255)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 2 },
                        Stepped = true,
                        Fill = true,
                        Order = 2
                    },
                    new LineChartDataset()
                    {
                        Label = "cloudcover_high",
                        Data = cloudcoverHigh,
                        BackgroundColor = "rgba(0, 0, 155, 0.5)",
                        BorderColor = "rgb(0, 0, 155)",
                        BorderWidth = 2,
                        PointRadius = new List<double>() { 2 },
                        Stepped = true,
                        Fill = true,
                        Order = 3
                    }
                },
            };
        }

        private async Task RenderChartsAsync()
        {
            await GetWeatherData();

            var radiationChartOptions = new LineChartOptions();

            radiationChartOptions.Interaction.Mode = InteractionMode.Index;
            radiationChartOptions.Responsive = true;
            radiationChartOptions.Scales.Y = new ChartAxes() { Min = 0, Max = 800 };
            radiationChartOptions.Scales.X!.Title = new ChartAxesTitle { Text = "Time", Display = true };
            radiationChartOptions.Scales.Y!.Title = new ChartAxesTitle { Text = "Watt", Display = true };
            radiationChartOptions.MaintainAspectRatio = false;
            await radiationChart.InitializeAsync(chartData: radiationData, chartOptions: radiationChartOptions);
            var cloudcoverChartOptions = new LineChartOptions();

            cloudcoverChartOptions.Interaction.Mode = InteractionMode.Index;
            cloudcoverChartOptions.Responsive = true;
            cloudcoverChartOptions.Scales.Y = new ChartAxes() { Min = 0, Max = 100 };
            cloudcoverChartOptions.Scales.X!.Title = new ChartAxesTitle { Text = "Time", Display = true };
            cloudcoverChartOptions.Scales.Y!.Title = new ChartAxesTitle { Text = "Percent", Display = true };
            cloudcoverChartOptions.MaintainAspectRatio = false;
            await cloudcoverChart.InitializeAsync(chartData: cloudcoverData, chartOptions: cloudcoverChartOptions);

            isWeatherChartInitialized = true;
        }

        private async Task UpdateChartsAsync()
        {
            try
            {
                if (isWeatherChartInitialized && radiationChart != null)
                {
                    await GetWeatherData();
                    if (radiationData != null)
                    {
                        await radiationChart.UpdateValuesAsync(radiationData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in UpdateChartsAsync");
            }
        }
    }
}
