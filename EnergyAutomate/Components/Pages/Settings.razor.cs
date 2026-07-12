using BlazorBootstrap;
using EnergyAutomate.Services;
using Microsoft.AspNetCore.Components;

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
    public required EnergyAutomateService ApiServiceController { get; set; }

    [Inject]
    public required GrowattApiService GrowattServiceController { get; set; }

    [Inject]
    public required TIbberApiService TibberServiceController { get; set; }

    private string StatusText { get; set; } = "Settings are loaded from database at startup.";
    private string BackgroundServicesStatusText { get; set; } = "Services can be controlled here.";

    private async Task SaveSettingsAsync()
    {
        await ApiServiceController.ApiSaveRuntimeSettingsToDatabaseAsync();
        StatusText = $"Settings saved at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task ReloadSettingsAsync()
    {
        await ApiServiceController.ApiLoadRuntimeSettingsFromDatabaseAsync();
        StatusText = $"Settings reloaded at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartApiServiceAsync()
    {
        await ExecuteServiceActionAsync(() => ApiServiceController.StartServiceAsync());
        BackgroundServicesStatusText = $"API service started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopApiServiceAsync()
    {
        await ExecuteServiceActionAsync(() => ApiServiceController.StopServiceAsync());
        BackgroundServicesStatusText = $"API service stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartApiServiceAsync()
    {
        await ExecuteServiceActionAsync(() => ApiServiceController.RestartServiceAsync());
        BackgroundServicesStatusText = $"API service restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartGrowattServiceAsync()
    {
        await ExecuteServiceActionAsync(() => GrowattServiceController.StartServiceAsync());
        BackgroundServicesStatusText = $"Growatt service started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopGrowattServiceAsync()
    {
        await ExecuteServiceActionAsync(() => GrowattServiceController.StopServiceAsync());
        BackgroundServicesStatusText = $"Growatt service stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartGrowattServiceAsync()
    {
        await ExecuteServiceActionAsync(() => GrowattServiceController.RestartServiceAsync());
        BackgroundServicesStatusText = $"Growatt service restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StartTibberServiceAsync()
    {
        await ExecuteServiceActionAsync(() => TibberServiceController.StartServiceAsync());
        BackgroundServicesStatusText = $"Tibber service started at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task StopTibberServiceAsync()
    {
        await ExecuteServiceActionAsync(() => TibberServiceController.StopServiceAsync());
        BackgroundServicesStatusText = $"Tibber service stopped at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task RestartTibberServiceAsync()
    {
        await ExecuteServiceActionAsync(() => TibberServiceController.RestartServiceAsync());
        BackgroundServicesStatusText = $"Tibber service restarted at {DateTimeOffset.Now.LocalDateTime}.";
    }

    private async Task ExecuteServiceActionAsync(Func<Task> action)
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