using Tibber.Sdk;

namespace EnergyAutomate
{
    public class RealTimeMeasurementExtention : RealTimeMeasurement
    {
        public RealTimeMeasurementExtention() { }

        public RealTimeMeasurementExtention(RealTimeMeasurement measurement)
        {
            Timestamp = measurement.Timestamp;
            Power = measurement.Power;
            LastMeterConsumption = measurement.LastMeterConsumption;
            AccumulatedConsumption = measurement.AccumulatedConsumption;
            AccumulatedConsumptionLastHour = measurement.AccumulatedConsumptionLastHour;
            AccumulatedProduction = measurement.AccumulatedProduction;
            AccumulatedProductionLastHour = measurement.AccumulatedProductionLastHour;
            AccumulatedCost = measurement.AccumulatedCost;
            AccumulatedReward = measurement.AccumulatedReward;
            Currency = measurement.Currency;
            MinPower = measurement.MinPower;
            AveragePower = measurement.AveragePower;
            MaxPower = measurement.MaxPower;
            PowerProduction = measurement.PowerProduction;
            PowerReactive = measurement.PowerReactive;
            PowerProductionReactive = measurement.PowerProductionReactive;
            MinPowerProduction = measurement.MinPowerProduction;
            MaxPowerProduction = measurement.MaxPowerProduction;
            LastMeterProduction = measurement.LastMeterProduction;
            VoltagePhase1 = measurement.VoltagePhase1;
            VoltagePhase2 = measurement.VoltagePhase2;
            VoltagePhase3 = measurement.VoltagePhase3;
            CurrentPhase1 = measurement.CurrentPhase1;
            CurrentPhase2 = measurement.CurrentPhase2;
            CurrentPhase3 = measurement.CurrentPhase3;
            PowerFactor = measurement.PowerFactor;
            SignalStrength = measurement.SignalStrength;
        }

        public int SettingPowerLoadSeconds { get; set; }

        public int SettingLockSeconds { get; set; }

        public int SettingOffSetAvg { get; set; }

        public int AvgPowerValue{ get; set; }

        public int AvgPowerLoad { get; set; }

        public int AvgOutputValue { get; set; }
    }
}
