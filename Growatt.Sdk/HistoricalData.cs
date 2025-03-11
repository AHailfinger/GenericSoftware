namespace Growatt.OSS
{
    public class HistoricalData
    {
        public string EndDate { get; set; }
        public List<DeviceNoahHistoricalData> Datas { get; set; }
        public int Start { get; set; }
        public bool HaveNext { get; set; }
    }
}
