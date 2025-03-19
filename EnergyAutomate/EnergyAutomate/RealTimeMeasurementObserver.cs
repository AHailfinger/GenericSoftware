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
            ApiService.StartRealTimeMeasurementListener().Wait();
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"An error occured: {error}");
            ApiService.StartRealTimeMeasurementListener().Wait();
        }
            
        public void OnNext(RealTimeMeasurement value)
        {
            Console.WriteLine($"{value.Timestamp} - consumtion: {value.Power:N0} W production: {value.PowerProduction:N0} W; ");

            OnNewRealTimeMeasurement?.Invoke(this, value);

            var dbContext = GetDbContext();           
            var newRealTimeMeasurementExtention = new RealTimeMeasurementExtention(value) {
                SettingOffSetAvg = ApiServiceInfo.SettingOffsetAvg,
                SettingLockSeconds = ApiServiceInfo.SettingLockSeconds,
                SettingPowerLoadSeconds = ApiServiceInfo.SettingPowerLoadSeconds,
                AvgPowerValue = ApiServiceInfo.AvgPowerValue,
                AvgPowerLoad = ApiServiceInfo.AvgPowerLoad,
                AvgOutputValue = ApiServiceInfo.AvgOutputValue
            };
            ApiServiceInfo.RealTimeMeasurement.Add(newRealTimeMeasurementExtention);
            dbContext.RealTimeMeasurements.Add(newRealTimeMeasurementExtention); // Speichern in der Datenbank

            dbContext.SaveChanges(); // Änderungen speichern
        }

        private ApplicationDbContext GetDbContext()
        {
            return ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
    }
}
