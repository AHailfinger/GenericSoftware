using BlazorBootstrap;
using EnergyAutomate.Definitions;
using EnergyAutomate.Emulator;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using static Python.Runtime.TypeSpec;

namespace EnergyAutomate.Components.Pages
{
    public partial class Growatt
    {
        private readonly IEnumerable<TickMark> ApiPowerTickList = EnergyAutomateService.GenerateTickTickMarks(0, 900, 50);

        #region Properties

        [Inject]
        public required ApplicationDbContext ApplicationDbContext { get; set; }

        [Inject]
        public required NavigationManager NavigationManager { get; set; }

        [Inject]
        public required ILogger<Growatt> Logger { get; set; }

        [Inject]
        public required PythonWrapper PythonWrapper { get; set; }

        private int SmartPowerValue { get; set; } = 500;
        private int DefaultPowerValue { get; set; } = 250;

        #endregion Properties

        private async Task SetSmartPowerAsync(DeviceList device)
        {
            PythonWrapper.SetSmartPower(device, SmartPowerValue);
        }

        private async Task SetDefaultPowerAsync(DeviceList device)
        {
            PythonWrapper.SetDefaultPower(device, DefaultPowerValue);
        }

        private async Task ClearDeviceNoahTimeSegmentsAsync(DeviceList device)
        {
            // Clear all 9 time segments (types 1-9) by setting enable to 0
            for (int slot = 1; slot <= 9; slot++)
            {
                var query = new DeviceNoahSetTimeSegmentQuery
                {
                    DeviceSn = device.DeviceSn,
                    DeviceType = device.DeviceType,
                    Type = slot.ToString(),
                    Enable = "0",
                    StartTime = "00:00",
                    EndTime = "00:00",
                    Power = "0",
                    Repeat = ""
                };

                PythonWrapper.SetNoahTimeSegment(query);

                Logger.LogInformation(
                    "[TRACE] ClearAllNoahTimeSegments: Cleared time segment slot {Slot}",
                    slot
                );
            }
        }

        private async Task BattPriorityDeviceNoahAsync(DeviceList device)
        {
            var query = new DeviceNoahSetTimeSegmentQuery
            {
                DeviceSn = device.DeviceSn,
                DeviceType = device.DeviceType,
                Type = "1", // Assuming type 1 is for battery priority
                Enable = "0",
                StartTime = "00:00",
                EndTime = "00:00",
                Power = "0",
                Repeat = ""
            };

            PythonWrapper.SetNoahTimeSegment(query);
        }

        private async Task LoadPriorityDeviceNoahAsync(DeviceList device)
        {
            var query = new DeviceNoahSetTimeSegmentQuery
            {
                DeviceSn = device.DeviceSn,
                DeviceType = device.DeviceType,
                Type = "0", // Assuming type 0 is for load priority
                Enable = "0",
                StartTime = "00:00",
                EndTime = "00:00",
                Power = "0",
                Repeat = ""
            };

            PythonWrapper.SetNoahTimeSegment(query);
        }
    }
}
