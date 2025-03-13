using EnergyAutomate;
using EnergyAutomate.Data;
using Growatt.OSS;
using Tibber.Sdk;

public partial class ApiService
{
    public class RealTimeMeasurementObserver : IObserver<RealTimeMeasurement>
    {
        private IServiceProvider ServiceProvider { get; set; }

        public RealTimeMeasurementObserver(IServiceProvider serviceProvider, ApiServiceInfo apiServiceInfo)
        {
            ServiceProvider = serviceProvider;
            ApiServiceInfo = apiServiceInfo;
        }

        public delegate Task OnNewRealTimeMeasurementHandler(object sender, RealTimeMeasurement value);
        public event OnNewRealTimeMeasurementHandler? OnNewRealTimeMeasurement;

        public ApiServiceInfo ApiServiceInfo { get; set; }

        public void OnCompleted() =>
            Console.WriteLine("Real time measurement stream has been terminated. ");
        public void OnError(Exception error) =>
            Console.WriteLine($"An error occured: {error}");
        public void OnNext(RealTimeMeasurement value)
        {
            Console.WriteLine($"{value.Timestamp} - consumtion: {value.Power:N0} W production: {value.PowerProduction:N0} W; ");

            OnNewRealTimeMeasurement?.Invoke(this, value);

            var dbContext = GetDbContext();

            ApiServiceInfo.RealTimeMeasurements.Add(value);
            ApiServiceInfo.RealTimeMeasurementExtentions.Add(new RealTimeMeasurementExtention() { TimeStamp = value.Timestamp, UpperLimit = ApiServiceInfo.ApiUpperLimit, LowerLimit = ApiServiceInfo.ApiLowerLimit });
            dbContext.RealTimeMeasurements.Add(value); // Speichern in der Datenbank

            dbContext.SaveChanges(); // Änderungen speichern
        }

        private ApplicationDbContext GetDbContext()
        {
            return ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
    }
}
