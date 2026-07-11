using EnergyAutomate.Definitions;
using EnergyAutomate.Services.CodeFactory;
using System.Diagnostics;

namespace EnergyAutomate.Utilities
{
    public class DistributionManager
    {
        #region Public Constructors

        public DistributionManager(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        #endregion Public Constructors

        #region Properties

        private EnergyAutomateService ApiService => ServiceProvider.GetRequiredService<EnergyAutomateService>();
        private RuntimeCodeTemplateExecutor CodeTemplateExecutor => ServiceProvider.GetRequiredService<RuntimeCodeTemplateExecutor>();
        private ApiQueueWatchdog<IDeviceQuery> GrowattDeviceQueryQueueWatchdog => ServiceProvider.GetRequiredService<ApiQueueWatchdog<IDeviceQuery>>();
        private ILogger LoggerRTM => ServiceProvider.GetRequiredService<ILogger<DistributionManager>>();
        private IServiceProvider ServiceProvider { get; init; }

        #endregion Properties

        #region Public Methods

        public async Task HandleDistributionAsync(List<DeviceList> devices, int totalPower, bool prioritizeHighSoc, DateTimeOffset timestamp)
        {
            var factory = new EnergyDistributionManagerScriptFactory(
                new EnergyDistributionManagerEvent(timestamp, devices, totalPower, prioritizeHighSoc),
                ApiService,
                GrowattDeviceQueryQueueWatchdog,
                CodeTemplateExecutor,
                LoggerRTM);

            await CodeTemplateExecutor.ExecuteAsync(ApiService.ActiveDistributionManagerTemplateKey, factory);
            return;
        }

        /// <summary>
        /// Performs load balancing between devices based on their SoC values. Shifts power from
        /// devices with lower SoC to those with higher SoC.
        /// </summary>
        /// <param name="devices">List of devices to balance</param>
        /// <param name="timestamp">Current timestamp</param>
        public async Task PerformLoadBalancing(List<DeviceList> devices, DateTimeOffset timestamp)
        {
            LoggerRTM.LogTrace("Starting load balancing for {DeviceCount} devices", devices.Count);

            if (devices.Count <= 1)
            {
                LoggerRTM.LogTrace("Load balancing not possible with 1 or 0 devices.");
                return;
            }

            var maxSocDev = devices.OrderByDescending(d => d.Soc).First();
            var minSocDev = devices.OrderBy(d => d.Soc).First();

            if (minSocDev.Soc == 0 || maxSocDev.Soc == 0)
            {
                Debugger.Break();
            }

            LoggerRTM.LogTrace("Load balancing check: MaxSoC Device {MaxDevSn} ({MaxSoC}%), MinSoC Device {MinDevSn} ({MinSoC}%)",
                maxSocDev.DeviceSn, maxSocDev.Soc, minSocDev.DeviceSn, minSocDev.Soc);

            // Only balance if SoC difference is significant (≥ 5%)
            if (maxSocDev.Soc - minSocDev.Soc >= 5)
            {
                int shift = 10; // Shift 10 W per adjustment cycle
                int maxPowerPerDevice = ApiService.ApiSettingMaxPower / devices.Count;
                int maxShiftUp = maxPowerPerDevice - maxSocDev.PowerValueCommited;
                int maxShiftDown = minSocDev.PowerValueCommited;

                LoggerRTM.LogTrace("Load balancing possible. MaxShiftUp: {MaxShiftUp}W, MaxShiftDown: {MaxShiftDown}W",
                    maxShiftUp, maxShiftDown);

                int actualShift = Math.Min(shift, Math.Min(maxShiftUp, maxShiftDown));

                if (actualShift > 0)
                {
                    LoggerRTM.LogTrace("Load balancing: Shifting {Shift}W from Device {MinDev} (SoC {MinSoc}%) to Device {MaxDev} (SoC {MaxSoc}%)",
                        actualShift, minSocDev.DeviceSn, minSocDev.Soc, maxSocDev.DeviceSn, maxSocDev.Soc);

                    var minSocDevPowerValueRequested = minSocDev.PowerValueCommited - actualShift;
                    var maxSocDevPowerValueRequested = maxSocDev.PowerValueCommited + actualShift;

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = minSocDev.DeviceSn,
                        Value = minSocDevPowerValueRequested,
                        Force = true,
                        TS = timestamp
                    });

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = maxSocDev.DeviceSn,
                        Value = maxSocDevPowerValueRequested,
                        Force = true,
                        TS = timestamp
                    });

                    LoggerRTM.LogTrace("Load balancing completed. Device {MinDev} set to {MinPower}W, Device {MaxDev} set to {MaxPower}W",
                        minSocDev.DeviceSn, minSocDevPowerValueRequested, maxSocDev.DeviceSn, maxSocDevPowerValueRequested);
                }
                else
                {
                    LoggerRTM.LogTrace("Load balancing: No room for shifting (actualShift={ActualShift}).", actualShift);
                }
            }
            else
            {
                LoggerRTM.LogTrace("Load balancing: SoC difference {SocDiff}% is <5%, no action.",
                    maxSocDev.Soc - minSocDev.Soc);
            }
        }


        /// <summary>Sets all devices to a specified power value.</summary>
        /// <param name="devices">List of devices to configure</param>
        /// <param name="powerValue">Power value to set for each device</param>
        /// <param name="timestamp">Current timestamp</param>
        public async Task SetAllDevicesToPower(List<DeviceList> devices, int powerValue, DateTimeOffset timestamp)
        {
            foreach (var device in devices)
            {
                LoggerRTM.LogTrace("Setting device {DeviceSn} (SoC {Soc}%) to {Power}W",
                    device.DeviceSn, device.Soc, powerValue);

                await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                {
                    DeviceType = "noah",
                    DeviceSn = device.DeviceSn,
                    Value = powerValue,
                    Force = true,
                    TS = timestamp
                });
            }

            LoggerRTM.LogTrace("All {DeviceCount} devices set to {Power}W", devices.Count, powerValue);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>Calculates weighted power distribution based on device capabilities</summary>
        private Dictionary<string, int> CalculateWeightedDistribution(List<DeviceList> devices, int totalPower, int maxPowerPerDevice)
        {
            Dictionary<string, int> allocations = new Dictionary<string, int>();
            int deviceCount = devices.Count;

            if (deviceCount == 0)
                return allocations;

            // Calculate total weighting factors
            int totalWeightingFactor = 0;
            Dictionary<string, int> deviceWeights = new Dictionary<string, int>();

            foreach (var device in devices)
            {
                if (device.DeviceSn != null)
                {
                    // Weight based on solar input and battery status
                    int weight = 100; // Base weight

                    // Add bonus weight for higher solar input
                    weight += (int)(device.PowerValueSolar / 10);

                    // Add bonus/penalty for battery status
                    if (device.PowerValueBatteryPower > 0) // Charging
                        weight += (int)(device.PowerValueBatteryPower / 20);
                    else if (device.PowerValueBatteryPower < 0) // Discharging
                        weight -= (int)(Math.Abs(device.PowerValueBatteryPower) / 20);

                    // Ensure minimum weight
                    weight = Math.Max(10, weight);

                    deviceWeights[device.DeviceSn] = weight;
                    totalWeightingFactor += weight;
                }
            }

            // First pass: calculate weighted allocation
            int remainingPower = totalPower;
            foreach (var device in devices)
            {
                if (device.DeviceSn != null)
                {
                    if (totalWeightingFactor == 0)
                    {
                        // Fallback to equal distribution
                        allocations[device.DeviceSn] = Math.Min(maxPowerPerDevice, totalPower / deviceCount);
                        continue;
                    }

                    double weightRatio = (double)deviceWeights[device.DeviceSn] / totalWeightingFactor;
                    int initialAllocation = (int)(totalPower * weightRatio);

                    // Cap at maxPowerPerDevice
                    int allocation = Math.Min(initialAllocation, maxPowerPerDevice);
                    // Ensure we don't exceed remaining power
                    allocation = Math.Min(allocation, remainingPower);

                    allocations[device.DeviceSn] = allocation;
                    remainingPower -= allocation;
                }
            }

            // Second pass: distribute any remaining power
            if (remainingPower > 0)
            {
                foreach (var device in devices.Where(x => x.DeviceSn != null).OrderByDescending(d => deviceWeights[d.DeviceSn!]))
                {
                    if (device.DeviceSn != null)
                    {
                        int additionalPower = Math.Min(remainingPower, maxPowerPerDevice - allocations[device.DeviceSn]);
                        if (additionalPower > 0)
                        {
                            allocations[device.DeviceSn] += additionalPower;
                            remainingPower -= additionalPower;

                            if (remainingPower <= 0)
                                break;
                        }
                    }
                }
            }

            return allocations;
        }

        /// <summary>
        /// Distributes the specified total power among the available devices based on SoC
        /// prioritization and special rules.
        /// </summary>
        /// <param name="devices">List of devices</param>
        /// <param name="totalPower">Total power to distribute</param>
        /// <param name="prioritizeHighSoc">
        /// If true, prioritize devices with higher SoC; otherwise prioritize lower SoC
        /// </param>
        /// <param name="timestamp">Current timestamp</param>
        private async Task DistributePower1(List<DeviceList> devices, int totalPower, bool prioritizeHighSoc, DateTimeOffset timestamp)
        {
            LoggerRTM.LogTrace("DistributePower1 called. Device count: {DeviceCount}, Total power: {TotalPower}W", devices.Count, totalPower);

            if (devices == null || devices.Count == 0)
            {
                LoggerRTM.LogTrace("No devices available for distribution.");
                return;
            }

            var device = devices.First();

            LoggerRTM.LogTrace("Direct control: Setting device {DeviceSn} (SoC {Soc}%) to {Power}W", device.DeviceSn, device.Soc, totalPower);

            await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
            {
                DeviceType = "noah",
                DeviceSn = device.DeviceSn,
                Value = totalPower,
                Force = true,
                TS = timestamp
            });

            LoggerRTM.LogTrace("DistributePower1 completed. Device {DeviceSn} set to {Power}W", device.DeviceSn, totalPower);
        }

        /// <summary>
        /// Distributes the specified total power among the available devices based on SoC
        /// prioritization and special rules.
        /// </summary>
        /// <param name="devices">List of devices</param>
        /// <param name="totalPower">Total power to distribute</param>
        /// <param name="prioritizeHighSoc">
        /// If true, prioritize devices with higher SoC; otherwise prioritize lower SoC
        /// </param>
        /// <param name="timestamp">Current timestamp</param>
        private async Task DistributePower2(List<DeviceList> devices, int totalPower, bool prioritizeHighSoc, DateTimeOffset timestamp)
        {
            int maxPowerPerDevice = devices.Count > 0 ? ApiService.ApiSettingMaxPower / devices.Count : 0;

            LoggerRTM.LogTrace("DistributePower started. Total power: {TotalPower}W, prioritize high SoC: {PrioritizeHighSoc}",
                totalPower, prioritizeHighSoc);

            // Special rule: If power requirement is less than 200, use only one device, set others
            // to 0
            if (totalPower < 200 && devices.Count > 1)
            {
                var mainDevice = devices.OrderByDescending(d => d.Soc).First();
                LoggerRTM.LogTrace("Power requirement < 200W. Only device {DeviceSn} will be used, others set to 0.", mainDevice.DeviceSn);

                foreach (var device in devices)
                {
                    int setPower = device == mainDevice ? totalPower : 0;
                    LoggerRTM.LogTrace("Setting device {DeviceSn} to {Power}W due to low power requirement.", device.DeviceSn, setPower);

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = device.DeviceSn,
                        Value = setPower,
                        Force = true,
                        TS = timestamp
                    });
                }
                LoggerRTM.LogTrace("Low power rule applied. Only one device active.");
                return;
            }

            // For charging devices: calculate possible charging power as (solarPower - chargingPower)
            foreach (var device in devices)
            {
                if (device.PowerValueBatteryPower > 0) // Device is charging
                {
                    int possibleChargingPower = device.PowerValueSolar - device.PowerValueBatteryPower;
                    LoggerRTM.LogTrace("Device {DeviceSn} is charging. Possible charging power: {PossiblePower}W (Solar {Solar}W - Charging {Charging}W)",
                        device.DeviceSn, possibleChargingPower, device.PowerValueSolar, device.PowerValueBatteryPower);
                    // This value can be used for further logic if needed
                }
            }

            // --- Original logic below ---

            int currentTotalPower = devices.Sum(d => d.PowerValueCommited);
            int powerDifference = totalPower - currentTotalPower;

            LoggerRTM.LogTrace("Current total power: {CurrentPower}W, target power: {TargetPower}W, difference: {Difference}W",
                currentTotalPower, totalPower, powerDifference);

            var eligibleDevices = devices
                .Where(d =>
                    (d.PowerValueSolar > 0 || !d.IsBatteryEmpty) &&
                    (
                        d.PowerValueBatteryStatus != 1
                        || (d.PowerValueBatteryStatus != 1 && (d.PowerValueSolar - d.PowerValueBatteryPower) > 100)
                    )
                )
                .ToList();

            foreach (var device in eligibleDevices)
            {
                LoggerRTM.LogTrace("Device {DeviceSn}: PowerOutput={Output}W, PowerSolar={Solar}W, PowerBattery={Battery}W, SoC={Soc}%",
                    device.DeviceSn, device.PowerValueOutput, device.PowerValueSolar, device.PowerValueBatteryPower, device.Soc);
            }

            LoggerRTM.LogTrace("Found {EligibleCount} eligible devices (non-empty) from {TotalCount} total devices",
                eligibleDevices.Count, devices.Count);

            if (devices.Any(d => d.IsBatteryEmpty))
            {
                var emptyDevices = devices.Where(d => d.IsBatteryEmpty).ToList();
                foreach (var device in emptyDevices)
                {
                    LoggerRTM.LogTrace("Device {DeviceSn} excluded from power distribution: Battery empty (SoC {Soc}%)",
                        device.DeviceSn, device.Soc);

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = device.DeviceSn,
                        Value = 0,
                        Force = true,
                        TS = timestamp
                    });
                }
            }

            if (!eligibleDevices.Any())
            {
                LoggerRTM.LogTrace("No eligible devices for power distribution. Aborting.");
                return;
            }

            if (Math.Abs(powerDifference) > 0 && Math.Abs(powerDifference) <= maxPowerPerDevice && eligibleDevices.Count > 1)
            {
                var optimalDevice = SelectOptimalDevice(eligibleDevices, powerDifference > 0, prioritizeHighSoc);

                int newPower = optimalDevice.PowerValueCommited;
                if (powerDifference > 0)
                {
                    int maxIncrease = Math.Min(maxPowerPerDevice - optimalDevice.PowerValueCommited,
                        Math.Max(0, GetDeviceAvailableCapacity(optimalDevice)));
                    int actualIncrease = Math.Min(powerDifference, maxIncrease);
                    newPower += actualIncrease;

                    LoggerRTM.LogTrace("Device {DeviceSn} can handle increase: Current {Current}W, Max increase {MaxIncrease}W, Actual increase {ActualIncrease}W, Solar {Solar}W, Battery {Battery}W",
                        optimalDevice.DeviceSn, optimalDevice.PowerValueCommited, maxIncrease, actualIncrease, optimalDevice.PowerValueSolar, optimalDevice.PowerValueBatteryPower);
                }
                else
                {
                    int maxDecrease = Math.Min(optimalDevice.PowerValueCommited, optimalDevice.PowerValueOutput);
                    int actualDecrease = Math.Min(Math.Abs(powerDifference), maxDecrease);
                    newPower -= actualDecrease;

                    LoggerRTM.LogTrace("Device {DeviceSn} can handle decrease: Current {Current}W, Max decrease {MaxDecrease}W, Actual decrease {ActualDecrease}W, Output {Output}W",
                        optimalDevice.DeviceSn, optimalDevice.PowerValueCommited, maxDecrease, actualDecrease, optimalDevice.PowerValueOutput);
                }

                if (newPower != optimalDevice.PowerValueCommited)
                {
                    LoggerRTM.LogTrace("Single device adjustment: Updating device {DeviceSn} (SoC {Soc}%) from {OldPower}W to {NewPower}W",
                        optimalDevice.DeviceSn, optimalDevice.Soc, optimalDevice.PowerValueCommited, newPower);

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = optimalDevice.DeviceSn,
                        Value = newPower,
                        Force = true,
                        TS = timestamp
                    });

                    LoggerRTM.LogTrace("Power adjustment completed using single device");
                    return;
                }
                else
                {
                    LoggerRTM.LogTrace("Single device adjustment not possible due to limits. Using balanced distribution.");
                }
            }

            var sortedDevices = SortDevicesByPriority(eligibleDevices, prioritizeHighSoc);

            LoggerRTM.LogTrace("Devices sorted by priority for balanced distribution");

            int powerPerDevice = eligibleDevices.Count > 0 ? totalPower / eligibleDevices.Count : 0;

            LoggerRTM.LogTrace("Base power per device: {PowerPerDevice}W, Max power per device: {MaxPowerPerDevice}W",
                powerPerDevice, maxPowerPerDevice);

            Dictionary<string, int> allocations = CalculateWeightedDistribution(sortedDevices, totalPower, maxPowerPerDevice);

            foreach (var device in sortedDevices)
            {
                if (device.DeviceSn != null)
                {
                    int allocatedPower = allocations[device.DeviceSn];

                    int powerChange = allocatedPower - device.PowerValueCommited;
                    LoggerRTM.LogTrace("Device {DeviceSn}: Current {Current}W, Allocated {Allocated}W, Change {Change}W, SoC {Soc}%, Output {Output}W, Solar {Solar}W, Battery {Battery}W",
                        device.DeviceSn, device.PowerValueCommited, allocatedPower, powerChange, device.Soc,
                        device.PowerValueOutput, device.PowerValueSolar, device.PowerValueBatteryPower);

                    await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
                    {
                        DeviceType = "noah",
                        DeviceSn = device.DeviceSn,
                        Value = allocatedPower,
                        Force = true,
                        TS = timestamp
                    });
                }
            }

            LoggerRTM.LogTrace("Balanced power distribution completed for {DeviceCount} devices", sortedDevices.Count);
        }

        /// <summary>
        /// Gets the available capacity for power increase based on solar and battery metrics
        /// </summary>
        private int GetDeviceAvailableCapacity(DeviceList device)
        {
            // This estimates how much more power the device could potentially provide based on
            // solar input and battery status
            return device.PowerValueSolar + (device.PowerValueBatteryPower > 0 ? device.PowerValueBatteryPower : 0);
        }

        /// <summary>Selects the optimal device for power adjustment based on real-time metrics</summary>
        private DeviceList SelectOptimalDevice(List<DeviceList> devices, bool isIncrease, bool prioritizeHighSoc)
        {
            if (isIncrease)
            {
                // For power increase, select device with:
                // 1. Appropriate SoC (high or low based on prioritization)
                // 2. High solar input (more efficient)
                // 3. Good battery status
                return prioritizeHighSoc
                    ? devices.OrderByDescending(d => d.Soc)
                            .ThenByDescending(d => d.PowerValueSolar)
                            .ThenByDescending(d => d.PowerValueBatteryPower)
                            .First()
                    : devices.OrderBy(d => d.Soc)
                            .ThenByDescending(d => d.PowerValueSolar)
                            .ThenByDescending(d => d.PowerValueBatteryPower)
                            .First();
            }
            else
            {
                // For power decrease, select device with:
                // 1. Appropriate SoC (high or low based on prioritization)
                // 2. Highest battery consumption (most negative PowerValueBattery)
                // - This will prioritize devices that are actively discharging their batteries
                return prioritizeHighSoc
                    ? devices.OrderByDescending(d => d.Soc)
                            .ThenBy(d => d.PowerValueBatteryPower) // Ascending to prioritize negative values
                            .First()
                    : devices.OrderBy(d => d.Soc)
                            .ThenBy(d => d.PowerValueBatteryPower) // Ascending to prioritize negative values
                            .First();
            }
        }

        /// <summary>Sorts devices by priority for power distribution</summary>
        private List<DeviceList> SortDevicesByPriority(List<DeviceList> devices, bool prioritizeHighSoc)
        {
            if (prioritizeHighSoc)
            {
                // When prioritizing high SoC (typically for increasing power): Sort by SoC
                // descending, then by solar input descending
                return devices.OrderByDescending(d => d.Soc)
                             .ThenByDescending(d => d.PowerValueSolar)
                             .ToList();
            }
            else
            {
                // When prioritizing low SoC (typically for decreasing power): Sort by SoC
                // ascending, and prioritize those with higher battery consumption (negative values)
                return devices.OrderBy(d => d.Soc)
                             .ThenBy(d => d.PowerValueBatteryPower) // Prioritize devices that are discharging more
                             .ToList();
            }
        }

        #endregion Private Methods
    }
}
