using Growatt.OSS;
using Newtonsoft.Json;


/// <summary>
/// Represents the information of a device.
/// </summary>
public class DeviceNoahInfo
{
    /// <summary>
    /// Device serial number.
    /// </summary>
    public string DeviceSn { get; set; }

    /// <summary>
    /// Data logger serial number.
    /// </summary>
    public string DatalogSn { get; set; }

    /// <summary>
    /// Associated inverter serial number.
    /// </summary>
    public string AssociatedInvSn { get; set; }

    /// <summary>
    /// Port name.
    /// </summary>
    public string PortName { get; set; }

    /// <summary>
    /// Alias of the device.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Location of the device.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Indicates if the device is lost.
    /// </summary>
    public bool Lost { get; set; }

    /// <summary>
    /// Address of the device.
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// Last update time in milliseconds since epoch.
    /// </summary>
    public long LastUpdateTime { get; set; }

    /// <summary>
    /// System time in milliseconds since epoch.
    /// </summary>
    public long SysTime { get; set; }

    /// <summary>
    /// Status of the device.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// High limit of charging SOC (State of Charge).
    /// </summary>
    public int ChargingSocHighLimit { get; set; }

    /// <summary>
    /// Low limit of charging SOC (State of Charge).
    /// </summary>
    public int ChargingSocLowLimit { get; set; }

    /// <summary>
    /// Default power of the device.
    /// </summary>
    public int DefaultPower { get; set; }

    /// <summary>
    /// Component power of the device.
    /// </summary>
    public double ComponentPower { get; set; }

    public string Time1Start { get; set; }
    public string Time1End { get; set; }
    public int Time1Mode { get; set; }
    public int Time1Power { get; set; }
    public string Time2Start { get; set; }
    public string Time2End { get; set; }
    public int Time2Mode { get; set; }
    public int Time2Power { get; set; }
    public string Time3Start { get; set; }
    public string Time3End { get; set; }
    public int Time3Mode { get; set; }
    public int Time3Power { get; set; }
    public string Time4Start { get; set; }
    public string Time4End { get; set; }
    public int Time4Mode { get; set; }
    public int Time4Power { get; set; }
    public string Time5Start { get; set; }
    public string Time5End { get; set; }
    public int Time5Mode { get; set; }
    public int Time5Power { get; set; }
    public string Time6Start { get; set; }
    public string Time6End { get; set; }
    public int Time6Mode { get; set; }
    public int Time6Power { get; set; }
    public string Time7Start { get; set; }
    public string Time7End { get; set; }
    public int Time7Mode { get; set; }
    public int Time7Power { get; set; }
    public string Time8Start { get; set; }
    public string Time8End { get; set; }
    public int Time8Mode { get; set; }
    public int Time8Power { get; set; }
    public string Time9Start { get; set; }
    public string Time9End { get; set; }
    public int Time9Mode { get; set; }
    public int Time9Power { get; set; }
    public int Time1Enable { get; set; }
    public int Time2Enable { get; set; }
    public int Time3Enable { get; set; }
    public int Time4Enable { get; set; }
    public int Time5Enable { get; set; }
    public int Time6Enable { get; set; }
    public int Time7Enable { get; set; }
    public int Time8Enable { get; set; }
    public int Time9Enable { get; set; }

    /// <summary>
    /// Smart socket power.
    /// </summary>
    public double SmartSocketPower { get; set; }

    /// <summary>
    /// OTA device type code high.
    /// </summary>
    public string OtaDeviceTypeCodeHigh { get; set; }

    /// <summary>
    /// OTA device type code low.
    /// </summary>
    public string OtaDeviceTypeCodeLow { get; set; }

    /// <summary>
    /// Model of the device.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Firmware version of the device.
    /// </summary>
    public string FwVersion { get; set; }

    /// <summary>
    /// MPPT version of the device.
    /// </summary>
    public string MpptVersion { get; set; }

    /// <summary>
    /// PD version of the device.
    /// </summary>
    public string PdVersion { get; set; }

    /// <summary>
    /// BMS version of the device.
    /// </summary>
    public string BmsVersion { get; set; }

    /// <summary>
    /// EBM order number.
    /// </summary>
    public int EbmOrderNum { get; set; }

    /// <summary>
    /// Temperature type.
    /// </summary>
    public int TempType { get; set; }

    /// <summary>
    /// Last update time as text.
    /// </summary>
    public string LastUpdateTimeText { get; set; }

    [JsonIgnore]
    public List<DeviceTimeSegment> TimeSegments
    {
        get
        {
            var result = new List<DeviceTimeSegment>();

            for (int i = 1; i <= 9; i++)
            {
                var startTime = GetType().GetProperty($"Time{i}Start")?.GetValue(this)?.ToString();
                var endTime = GetType().GetProperty($"Time{i}End")?.GetValue(this)?.ToString();
                var mode = GetType().GetProperty($"Time{i}Mode")?.GetValue(this)?.ToString();
                var power = GetType().GetProperty($"Time{i}Power")?.GetValue(this)?.ToString();
                var enable = GetType().GetProperty($"Time{i}Enable")?.GetValue(this)?.ToString();

                if (startTime != null && endTime != null && mode != null && power != null && enable != null)
                {
                    result.Add(new DeviceTimeSegment
                    {
                        DeviceType = "noah",
                        DeviceSn = DeviceSn,
                        Type = i.ToString(),
                        StartTime = startTime,
                        EndTime = endTime,
                        Mode = mode,
                        Power = power,
                        Enable = enable
                    });
                }
            }

            return result;
        }
    }

}
