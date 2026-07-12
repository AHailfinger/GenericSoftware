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

        private WeatherForecast? DashboardWeatherForecast { get; set; }

        #endregion Fields

        #region Properties

        [CascadingParameter]
        private MainLayout? MainLayout { get; set; }

        #endregion Properties

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
    }
}
