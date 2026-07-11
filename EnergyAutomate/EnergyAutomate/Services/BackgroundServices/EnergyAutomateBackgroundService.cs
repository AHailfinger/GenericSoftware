using EnergyAutomate.Emulator.Shelly;

namespace EnergyAutomate.Services.BackgroundServices
{
    public class EnergyAutomateBackgroundService : IHostedService, IDisposable
    {
        #region Public Constructors

        public EnergyAutomateBackgroundService(EnergyAutomateService apiService, IConfiguration configuration, ILogger<EnergyAutomateBackgroundService> logger)
        {
            ApiService = apiService;
            Configuration = configuration;
            Logger = logger;
        }

        #endregion Public Constructors

        #region Properties

        private readonly SemaphoreSlim lifecycleLock = new(1, 1);
        private EnergyAutomateService ApiService { get; init; }
        private IConfiguration Configuration { get; init; }
        private ILogger<EnergyAutomateBackgroundService> Logger { get; init; }

        private ShellyPro3EMDevice? ShellyPro3EMDevice { get; set; }
        private bool isBusy;
        private bool isRunning;
        private string statusText = "Stopped";
        private string? lastError;

        public bool IsBusy => isBusy;
        public bool IsRunning => isRunning;
        public string? LastError => lastError;
        public string StatusText => statusText;

        #endregion Properties

        #region Public Methods

        public void Dispose()
        {
            lifecycleLock.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartServiceAsync(cancellationToken, respectConfiguration: true);
        }

        public async Task RestartServiceAsync(CancellationToken cancellationToken = default)
        {
            await StopServiceAsync(cancellationToken);
            await StartServiceAsync(cancellationToken, respectConfiguration: false);
        }

        public async Task StartServiceAsync(CancellationToken cancellationToken = default)
        {
            await StartServiceAsync(cancellationToken, respectConfiguration: false);
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

                Logger.LogTrace("Stopping ApiBackgroundService");

                try
                {
                    await ApiService.ApiStopAsync(cancellationToken);
                    isRunning = false;
                    statusText = "Stopped";
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    statusText = "Stopped";
                    Logger.LogInformation("ApiBackgroundService stop was canceled");
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    statusText = "Failed";
                    Logger.LogError(ex, "ApiBackgroundService stop failed");
                }
            }
            finally
            {
                isBusy = false;
                lifecycleLock.Release();
            }
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

                if (respectConfiguration && !Configuration.GetSection("BackgroundServices").GetValue("ApiBackgroundService", true))
                {
                    statusText = "Disabled by configuration";
                    return;
                }

                isBusy = true;
                statusText = "Starting";
                lastError = null;

                Logger.LogTrace("Starting ApiBackgroundService");

                //var proxy = new MqttProxy(
                //    proxyCertPath: "certs/server.crt",
                //    proxyKeyPath: "certs/server.key",
                //    brokerHost: "mqtt.growatt.com",
                //    brokerPort: 7006);

                //await proxy.StartAsync();

                //var device = new ShellyPro3EMDevice();
                //var udpServer = new ShellyPro3EMUdpServer(1010, device); // UDP-Port wie bei Shelly-CoAP

                // _ = Task.Run(udpServer.StartAsync);

                try
                {
                    await ApiService.ApiStartAsync(cancellationToken);
                    isRunning = true;
                    statusText = "Running";
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    statusText = "Stopped";
                    Logger.LogInformation("ApiService startup was canceled");
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    statusText = "Failed";
                    Logger.LogError(ex, "ApiService startup failed. Continuing without cached startup data.");
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

        #endregion Public Methods
    }
}
