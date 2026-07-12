using EnergyAutomate.Services;

namespace EnergyAutomate.Watchdogs
{
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>, IDisposable
    {
        #region Public Constructors

        public RealTimeMeasurementObserver(TIbberApiService apiRealTimeMeasurementWatchdog)
        {
            Watchdog = apiRealTimeMeasurementWatchdog;
        }

        #endregion Public Constructors

        #region Properties

        private TIbberApiService Watchdog { get; set; }

        #endregion Properties

        #region Public Methods

        public void Dispose()
        {
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
            _ = Watchdog.ProcessRealTimeMeasurementAsync(value);
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
