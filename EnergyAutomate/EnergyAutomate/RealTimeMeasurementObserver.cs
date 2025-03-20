using EnergyAutomate;
using EnergyAutomate.Data;
using Growatt.OSS;
using Tibber.Sdk;

public partial class ApiService
{
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>
    {
        private IServiceProvider ServiceProvider { get; set; }

        public RealTimeMeasurementObserver(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public delegate Task OnNewRealTimeMeasurementHandler(object sender, RealTimeMeasurement value);
        public event OnNewRealTimeMeasurementHandler? OnNewRealTimeMeasurement;

        public ApiServiceInfo ApiServiceInfo => ServiceProvider.GetRequiredService<ApiServiceInfo>();

        public ApiService ApiService => ServiceProvider.GetRequiredService<ApiService>();

        public void OnCompleted() {
            Console.WriteLine("Real time measurement stream has been terminated. ");
            _ = ApiService.StartRealTimeMeasurementListener();
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"An error occured: {error}");
            _ = ApiService.StartRealTimeMeasurementListener();
        }
            
        public void OnNext(RealTimeMeasurement value)
        {
            Console.WriteLine($"{value.Timestamp} - consumtion: {value.Power:N0} W production: {value.PowerProduction:N0} W; ");

            ApiServiceInfo.AvgPowerLoad = BerechneDurchschnittswertDerLetztenXSekunden(ApiServiceInfo.SettingPowerLoadSeconds);

            OnNewRealTimeMeasurement?.Invoke(this, value);

            var dbContext = ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var newRealTimeMeasurementExtention = new RealTimeMeasurementExtention(value)
            {
                TS = value.Timestamp.DateTime,
                DeviceInfos = [.. ApiServiceInfo.OutputValueValueChange],
                SettingLockSeconds = ApiServiceInfo.SettingLockSeconds,
                SettingPowerLoadSeconds = ApiServiceInfo.SettingPowerLoadSeconds,
                AvgPowerLoad = ApiServiceInfo.AvgPowerLoad,
                SettingOffSetAvg = ApiServiceInfo.SettingOffsetAvg
            };
            ApiServiceInfo.RealTimeMeasurement.Add(newRealTimeMeasurementExtention);
            dbContext.RealTimeMeasurements.Add(newRealTimeMeasurementExtention); // Speichern in der Datenbank

            dbContext.SaveChanges(); // Änderungen speichern
        }

        public int BerechneDurchschnittswertDerLetztenXSekunden(int sec)
        {
            var now = DateTimeOffset.Now;
            var result = ApiServiceInfo.RealTimeMeasurement
                .Where(m => m.Timestamp >= now.AddSeconds(-sec))
                .ToList();

            if (!result.Any())
            {
                return 0;
            }

            var avg = result.Average(m => m.TotalPower);
            return (int)avg;
        }
    }
}
