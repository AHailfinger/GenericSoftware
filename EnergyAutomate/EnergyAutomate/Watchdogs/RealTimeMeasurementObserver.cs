using EnergyAutomate.Services.BackgroundServices;

namespace EnergyAutomate.Watchdogs
{
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>, IDisposable
    {
        #region Public Constructors

        public RealTimeMeasurementObserver(TIbberApiService apiRealTimeMeasurementWatchdog, TibberBackgroundService tibberBackgroundService)
        {
            TibberBackgroundService = tibberBackgroundService;
            Watchdog = apiRealTimeMeasurementWatchdog;
        }

        #endregion Public Constructors

        #region Properties

        private TibberBackgroundService? TibberBackgroundService { get; set; }

        private TIbberApiService Watchdog { get; set; }

        #endregion Properties

        #region Public Methods

        public void Dispose()
        {
            TibberBackgroundService = null;
        }

        public void OnCompleted()
        {
            _ = Task.Run(() => RestartListenerAsync(), CancellationToken.None);
        }

        public void OnError(Exception error)
        {
            _ = Task.Run(() => RestartListenerAsync(), CancellationToken.None);
        }

        public void OnNext(RealTimeMeasurement value)
        {
            _ = TibberBackgroundService?.ProcessRealTimeMeasurementAsync(value);
        }

        #endregion Public Methods

        #region Private Methods

        private async Task RestartListenerAsync()
        {
            if (!Watchdog.RestartRequested)
            {
                Watchdog.RestartRequested = true;
                await Watchdog.RestartListener();
            }
        }

        #endregion Private Methods
    }
}
