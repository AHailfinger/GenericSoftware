namespace Growatt.OSS
{
    public class DeviceTimeSegment
    {
        public string DeviceSn { get; set; }
        public string DeviceType { get; set; }
        public string Type { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Mode { get; set; }
        public string Power { get; set; }
        public string Enable { get; set; }

        public FormUrlEncodedContent ToFormUrlEncodedContent()
        {
            var keyValuePairs = new[]
            {
            new KeyValuePair<string, string>("deviceSn", DeviceSn),
            new KeyValuePair<string, string>("deviceType", DeviceType),
            new KeyValuePair<string, string>("type", Type),
            new KeyValuePair<string, string>("startTime", StartTime),
            new KeyValuePair<string, string>("endTime", EndTime),
            new KeyValuePair<string, string>("mode", Mode),
            new KeyValuePair<string, string>("power", Power),
            new KeyValuePair<string, string>("enable", Enable)
        };

            return new FormUrlEncodedContent(keyValuePairs);
        }
    }
}
