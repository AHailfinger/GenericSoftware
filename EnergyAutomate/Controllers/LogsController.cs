using EnergyAutomate.Definitions;
using EnergyAutomate.Logging;
using Microsoft.AspNetCore.Mvc;

namespace EnergyAutomate.Controllers
{
    /// <summary>
    /// API Controller für Zugriff auf Trace-Logs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly CustomLoggerProvider _loggerProvider;
        private readonly ILogger<LogsController> _logger;

        public LogsController(CustomLoggerProvider loggerProvider, ILogger<LogsController> logger)
        {
            _loggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ruft alle verfügbaren Log-Kategorien ab
        /// </summary>
        /// <returns>Liste der Log-Kategorien</returns>
        [HttpGet("categories")]
        public ActionResult<LogCategoriesDto> GetCategories()
        {
            try
            {
                var categories = _loggerProvider.GetLogCategories();
                return Ok(new LogCategoriesDto { Categories = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der Log-Kategorien");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Fehler beim Abrufen der Log-Kategorien" });
            }
        }

        /// <summary>
        /// Ruft alle Logs ab oder Logs einer spezifischen Kategorie
        /// </summary>
        /// <param name="category">Optionale Log-Kategorie (z.B. 'GrowattData.Parse')</param>
        /// <param name="limit">Maximale Anzahl von Logs (Standard: 50)</param>
        /// <returns>Log-Response mit Logs und Kategorien</returns>
        [HttpGet]
        public ActionResult<LogResponseDto> GetLogs([FromQuery] string? category = null, [FromQuery] int limit = 50)
        {
            try
            {
                if (limit < 1 || limit > 500)
                {
                    limit = 50;
                }

                var logs = _loggerProvider.GetLogMessages(category);
                var displayLogs = logs.Take(limit).ToList();

                var logDtos = displayLogs.Select(log => new LogDto
                {
                    Timestamp = log.TS.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),
                    Category = log.Category ?? "Unknown",
                    Message = log.Message,
                    LogLevel = log.LogLevel.ToString()
                }).ToList();

                var categories = _loggerProvider.GetLogCategories();

                var response = new LogResponseDto
                {
                    Logs = logDtos,
                    Categories = categories,
                    TotalCount = logs.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der Logs für Kategorie: {Category}", category ?? "alle");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Fehler beim Abrufen der Logs" });
            }
        }

        /// <summary>
        /// Ruft Logs einer spezifischen Kategorie ab
        /// </summary>
        /// <param name="category">Log-Kategorie</param>
        /// <param name="limit">Maximale Anzahl von Logs</param>
        /// <returns>Log-Response</returns>
        [HttpGet("{category}")]
        public ActionResult<LogResponseDto> GetLogsByCategory(string category, [FromQuery] int limit = 50)
        {
            return GetLogs(category, limit);
        }

        /// <summary>
        /// Prüft ob Logs verfügbar sind
        /// </summary>
        /// <returns>Status der Log-Verfügbarkeit</returns>
        [HttpGet("status/any")]
        public ActionResult<dynamic> HasLogs()
        {
            try
            {
                var hasLogs = _loggerProvider.LogMessagesAny;
                var categories = _loggerProvider.GetLogCategories();

                return Ok(new
                {
                    hasLogs,
                    categoriesCount = categories.Count,
                    categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Prüfen des Log-Status");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Fehler beim Prüfen des Log-Status" });
            }
        }

        /// <summary>
        /// Exportiert alle Logs als CSV
        /// </summary>
        /// <returns>CSV-Datei</returns>
        [HttpGet("export/csv")]
        public ActionResult ExportCsv()
        {
            try
            {
                var logs = _loggerProvider.GetLogMessages();
                var csv = new System.Text.StringBuilder();

                // Header
                csv.AppendLine("Timestamp,Category,LogLevel,Message");

                // Rows
                foreach (var log in logs)
                {
                    var timestamp = log.TS.ToString("yyyy-MM-dd HH:mm:ss.fff zzz").Replace(",", ";");
                    var category = (log.Category ?? "Unknown").Replace(",", ";");
                    var logLevel = log.LogLevel.ToString().Replace(",", ";");
                    var message = (log.Message ?? string.Empty).Replace(",", ";").Replace("\n", " ");

                    csv.AppendLine($"\"{timestamp}\",\"{category}\",\"{logLevel}\",\"{message}\"");
                }

                var content = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(content, "text/csv", "logs.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Exportieren der Logs als CSV");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Fehler beim Exportieren der Logs" });
            }
        }

        /// <summary>
        /// Exportiert alle Logs als JSON
        /// </summary>
        /// <returns>JSON-Datei</returns>
        [HttpGet("export/json")]
        public ActionResult ExportJson()
        {
            try
            {
                var logs = _loggerProvider.GetLogMessages();
                var logDtos = logs.Select(log => new LogDto
                {
                    Timestamp = log.TS.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),
                    Category = log.Category ?? "Unknown",
                    Message = log.Message,
                    LogLevel = log.LogLevel.ToString()
                }).ToList();

                var json = System.Text.Json.JsonSerializer.Serialize(logDtos, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var content = System.Text.Encoding.UTF8.GetBytes(json);

                return File(content, "application/json", "logs.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Exportieren der Logs als JSON");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Fehler beim Exportieren der Logs" });
            }
        }
    }
}
