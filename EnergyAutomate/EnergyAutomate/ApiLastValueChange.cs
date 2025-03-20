using System.Text.Json.Serialization;

namespace EnergyAutomate
{
    public class ApiOutputValueChange
    {
        public DateTime TS { get; set; }

        [JsonInclude]
        public List<ApiOutputValueDeviceInfo> DeviceInfos { get; set; } = new List<ApiOutputValueDeviceInfo>();
    }

    public class ApiOutputValueDeviceInfo
    {
        public DateTime TS { get; set; }

        public string DeviceSn { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public int Value { get; set; }
    }
}
