namespace EnergyAutomate.Definitions
{
    /// <summary>
    /// DTO für ein einzelnes Log-Eintrag über die API
    /// </summary>
    public class LogDto
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string LogLevel { get; set; } = "Information";
    }

    /// <summary>
    /// DTO für die Log-Response über die API
    /// </summary>
    public class LogResponseDto
    {
        public List<LogDto> Logs { get; set; } = [];
        public List<string> Categories { get; set; } = [];
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// DTO für die verfügbaren Log-Kategorien
    /// </summary>
    public class LogCategoriesDto
    {
        public List<string> Categories { get; set; } = [];
    }
}
