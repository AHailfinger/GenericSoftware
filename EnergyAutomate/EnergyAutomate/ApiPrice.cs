using Tibber.Sdk;

namespace EnergyAutomate
{
    public class ApiPrice
    {
        public decimal? Total { get; set; }
        public DateTime StartsAt { get; set; }
        public PriceLevel? Level { get; set; }
    }
}
