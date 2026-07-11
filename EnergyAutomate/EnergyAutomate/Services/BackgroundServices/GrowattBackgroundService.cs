using EnergyAutomate.Emulator;
using EnergyAutomate.Emulator.Growatt;
using EnergyAutomate.Emulator.Growatt.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnergyAutomate.Services.BackgroundServices
{
    public class GrowattBackgroundService : IHostedService, IDisposable
    {
        private readonly SemaphoreSlim lifecycleLock = new(1, 1);
        private IServiceProvider ServiceProvider { get; set; }

        private ILogger<GrowattBackgroundService> Logger => ServiceProvider.GetRequiredService<ILogger<GrowattBackgroundService>>();

        private readonly PythonWrapper _pythonWrapper;
        private readonly IConfiguration _configuration;
        private bool isBusy;
        private bool isRunning;
        private string statusText = "Stopped";
        private string? lastError;

        public GrowattBackgroundService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _pythonWrapper = serviceProvider.GetRequiredService<PythonWrapper>();

            _pythonWrapper.GrowattClientOptions = _configuration.GetSection("GrowattClient").Get<GrowattClientOptions>()
                ?? throw new InvalidOperationException("GrowattClient configuration section is missing or invalid in appsettings.");
        }

        public bool IsBusy => isBusy;
        public bool IsRunning => isRunning;
        public string? LastError => lastError;
        public string StatusText => statusText;

        public async Task RestartServiceAsync(CancellationToken cancellationToken = default)
        {
            await StopServiceAsync(cancellationToken);
            await StartServiceAsync(cancellationToken, respectConfiguration: false);
        }

        public async Task StartServiceAsync(CancellationToken cancellationToken = default)
        {
            await StartServiceAsync(cancellationToken, respectConfiguration: false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartServiceAsync(cancellationToken, respectConfiguration: true);
        }

        public async Task StopServiceAsync(CancellationToken cancellationToken = default)
        {
            await lifecycleLock.WaitAsync(cancellationToken);
            try
            {
                if (!isRunning && !isBusy)
                {
                    statusText = "Stopped";
                    return;
                }

                isBusy = true;
                statusText = "Stopping";
                lastError = null;

                try
                {
                    Logger.LogTrace("Stopping MQTT proxy worker");
                    _pythonWrapper.StopPythonClient();
                    isRunning = false;
                    statusText = "Stopped";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    statusText = "Failed";
                    Logger.LogError(ex, "MQTT proxy worker failed to stop");
                }
            }
            finally
            {
                isBusy = false;
                lifecycleLock.Release();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopServiceAsync(cancellationToken);
        }

        private async Task StartServiceAsync(CancellationToken cancellationToken, bool respectConfiguration)
        {
            await lifecycleLock.WaitAsync(cancellationToken);
            try
            {
                if (isRunning)
                {
                    statusText = "Running";
                    return;
                }

                if (respectConfiguration && !(_configuration.GetSection("BackgroundServices").GetValue("GrowattApi", false)))
                {
                    statusText = "Disabled by configuration";
                    return;
                }

                isBusy = true;
                statusText = "Starting";
                lastError = null;

                try
                {
                    Logger.LogTrace("Starting MQTT proxy worker");
                    _pythonWrapper.StartPythonClient();
                    isRunning = true;
                    statusText = "Running";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    statusText = "Failed";
                    Logger.LogError(ex, "MQTT proxy worker failed to start");
                }
            }
            finally
            {
                isBusy = false;
                lifecycleLock.Release();
            }
        }

        public void Dispose()
        {
            lifecycleLock.Dispose();
        }

    }
}
