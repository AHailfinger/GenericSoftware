using Newtonsoft.Json;

namespace Growatt.OSS
{
    public class DeviceListData
    {
        public int Pages { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }

        [JsonProperty("data")]
        public List<Device>? Devices { get; set; }
    }
}
