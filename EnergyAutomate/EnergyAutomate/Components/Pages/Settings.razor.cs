using BlazorBootstrap;
using EnergyAutomate.Services.BackgroundServices;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace EnergyAutomate.Components.Pages;

public partial class Settings
{
    private readonly IEnumerable<TickMark> ApiOffsetAvgTickList = EnergyAutomateService.GenerateTickTickMarks(-25, 150, 5);
    private readonly IEnumerable<TickMark> ApiPowerTickList = EnergyAutomateService.GenerateTickTickMarks(0, 900, 50);
    private readonly IEnumerable<TickMark> ApiSettingPowerAdjustmentFactorTickList = EnergyAutomateService.GenerateTickTickMarks(0, 100, 10);
    private readonly IEnumerable<TickMark> ApiSettingPowerAdjustmentWaitCyclesTickList = EnergyAutomateService.GenerateTickTickMarks(0, 5, 1);
    private readonly IEnumerable<TickMark> ApiToleranceAvgTickList = EnergyAutomateService.GenerateTickTickMarks(0, 300, 10);
    private readonly IEnumerable<TickMark> AvgPowerLoadSecondsTickList = EnergyAutomateService.GenerateTickTickMarks(0, 180, 5);

    [Inject]
    public required RuntimeCodeTemplateStore TemplateStore { get; set; }

    [Inject]
    public required TIbberApiService RealTimeMeasurementWatchdog { get; set; }

    [Inject]
    public required EnergyAutomateBackgroundService ApiBackgroundServiceController { get; set; }

    [Inject]
    public required GrowattBackgroundService GrowattApiController { get; set; }

    [Inject]
    public required TibberBackgroundService TibberBackgroundServiceController { get; set; }

    private string StatusText { get; set; } = "Settings are loaded from database at startup.";
    private string BackgroundServicesStatusText { get; set; } = "Background services can be controlled here.";

    private async Task SaveSettingsAsync()
    {
        await ApiService.ApiSaveRuntimeSettingsToDatabaseAsync();
        StatusText = $"Settings saved at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task ReloadSettingsAsync()
    {
        await ApiService.ApiLoadRuntimeSettingsFromDatabaseAsync();
        StatusText = $"Settings reloaded at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartApiBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => ApiBackgroundServiceController.StartServiceAsync());
        BackgroundServicesStatusText = $"API background service started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopApiBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => ApiBackgroundServiceController.StopServiceAsync());
        BackgroundServicesStatusText = $"API background service stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartApiBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => ApiBackgroundServiceController.RestartServiceAsync());
        BackgroundServicesStatusText = $"API background service restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartGrowattApiAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => GrowattApiController.StartServiceAsync());
        BackgroundServicesStatusText = $"MQTT proxy worker started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopGrowattApiAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => GrowattApiController.StopServiceAsync());
        BackgroundServicesStatusText = $"MQTT proxy worker stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartGrowattApiAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => GrowattApiController.RestartServiceAsync());
        BackgroundServicesStatusText = $"MQTT proxy worker restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartTibberBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => TibberBackgroundServiceController.StartServiceAsync());
        BackgroundServicesStatusText = $"Tibber background service started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopTibberBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => TibberBackgroundServiceController.StopServiceAsync());
        BackgroundServicesStatusText = $"Tibber background service stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartTibberBackgroundServiceAsync()
    {
        await ExecuteBackgroundServiceActionAsync(() => TibberBackgroundServiceController.RestartServiceAsync());
        BackgroundServicesStatusText = $"Tibber background service restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task ExecuteBackgroundServiceActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string GetBackgroundServiceBadgeClass(string statusText)
    {
        return statusText switch
        {
            "Running" => "bg-success",
            "Starting" or "Stopping" => "bg-warning text-dark",
            "Disabled by configuration" => "bg-secondary",
            "Failed" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    private IEnumerable<CodeTemplateViewModel> GetTemplates(string topic)
    {
        return TemplateStore.GetTemplates()
            .Where(template => string.Equals(template.Topic, topic, StringComparison.OrdinalIgnoreCase))
            .OrderBy(template => template.DisplayName);
    }
}
