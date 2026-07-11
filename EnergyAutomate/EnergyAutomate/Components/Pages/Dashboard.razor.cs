using BlazorBootstrap;
using EnergyAutomate.Components.Layout;
using EnergyAutomate.Extentions;
using Microsoft.AspNetCore.Components;
using System.Data;
using OpenMeteo;

namespace EnergyAutomate.Components.Pages
{
    public partial class Dashboard
    {
        #region Fields

        private readonly IEnumerable<TickMark> ApiOffsetAvgTickList = EnergyAutomateService.GenerateTickTickMarks(-25, 150, 5);
        private readonly IEnumerable<TickMark> ApiSettingTimeOffsetTickList = EnergyAutomateService.GenerateTickTickMarks(-12, 12, 1);
        private readonly IEnumerable<TickMark> ApiToleranceAvgTickList = EnergyAutomateService.GenerateTickTickMarks(0, 300, 10);
        private readonly IEnumerable<TickMark> AvgPowerLoadSecondsTickList = EnergyAutomateService.GenerateTickTickMarks(0, 180, 5);
        private readonly IEnumerable<TickMark> ApiSettingPowerAdjustmentWaitCyclesTickList = EnergyAutomateService.GenerateTickTickMarks(0, 5, 1);
        private readonly IEnumerable<TickMark> ApiSettingPowerAdjustmentFactorTickList = EnergyAutomateService.GenerateTickTickMarks(0, 100, 10);
        private WeatherForecast? DashboardWeatherForecast { get; set; }

        #endregion Fields

        #region Properties

        [CascadingParameter]
        private MainLayout? MainLayout { get; set; }

        #endregion Properties

        #region Public Methods

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override async Task OnInitializedAsync()
        {
            DashboardWeatherForecast = ApiService.CurrentState.WeatherForecastToday ?? await ApiService.CurrentState.GetWeatherForecastAsync();
            ApiService.CurrentState.WeatherForecastToday ??= DashboardWeatherForecast;

            await base.OnInitializedAsync();
        }

        protected override void OnInitialized()
        {
            // No chart-specific subscriptions on the dashboard anymore.
        }

        #endregion Protected Methods

        #region Private Methods

        #endregion Private Methods
    }
}
