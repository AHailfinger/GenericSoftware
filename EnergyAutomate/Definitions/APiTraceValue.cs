namespace EnergyAutomate.Definitions
{
    public class APiTraceValue
    {
        public APiTraceValue()
        {
        }

        public APiTraceValue(DateTimeOffset? ts, int index, string key, string value)
        {
            TS = ts;
            Index = index;
            Key = key;
            Value = value;
        }

        #region Properties

        public DateTimeOffset? TS { get; set; }
        public int Index { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        #endregion Properties
    }
}
