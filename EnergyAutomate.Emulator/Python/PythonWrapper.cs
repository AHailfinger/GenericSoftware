using EnergyAutomate.Emulator.Growatt;
using EnergyAutomate.Emulator.Growatt.Models;
using EnergyAutomate.Emulator.Python;
using EnergyAutomate.Growatt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace EnergyAutomate.Emulator
{
    public class PythonWrapper
    {
        private readonly object pythonRuntimeLock = new();
        private GrowattModbusCodec ModbusCodec { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        private ILogger<PythonWrapper> Logger => ServiceProvider.GetRequiredService<ILogger<PythonWrapper>>();
        public GrowattClientOptions? GrowattClientOptions { get; set; }

        public delegate void LogCallback(string message);
        public delegate void DumpCallback(string topic, byte[] payload, int qos, int retain, int state, int dup, int mid);

        private Thread? _pythonThread;
        private dynamic? _clientInstance;

        public PythonWrapper(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ModbusCodec = serviceProvider.GetRequiredService<GrowattModbusCodec>();
            RegisterSignalHandlers();
        }

        private void RegisterSignalHandlers()
        {
            // Handle SIGTERM (Standard Docker Stop Signal)
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Logger.LogInformation("[TRACE] Process exit signal received");
                StopPythonClient();
            };

            // Handle SIGINT (Ctrl+C)
            Console.CancelKeyPress += (sender, e) =>
            {
                Logger.LogInformation("[TRACE] Cancel key pressed (SIGINT)");
                e.Cancel = true; // Verhindere sofortigen Prozessabbruch
                StopPythonClient();
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    // In .NET 9, wir können PosixSignalRegistration direkt verwenden
                    // SIGTERM
                    PosixSignalRegistration.Create(PosixSignal.SIGTERM, (context) =>
                    {
                        Logger.LogInformation("[TRACE] SIGTERM signal received");
                        StopPythonClient();
                        context.Cancel = true;
                    });

                    // SIGQUIT
                    PosixSignalRegistration.Create(PosixSignal.SIGQUIT, (context) =>
                    {
                        Logger.LogInformation("[TRACE] SIGQUIT signal received");
                        StopPythonClient();
                        context.Cancel = true;
                    });

                    Logger.LogInformation("[TRACE] POSIX signal handlers registered successfully");
                }
                catch (Exception ex)
                {
                    Logger.LogError("[TRACE] Failed to register POSIX signal handlers: {Exception}", ex);
                }
            }
        }

        public void StartPythonClient()
        {
            EnsurePythonRuntimeInitialized();

            if (_pythonThread?.IsAlive == true)
            {
                Logger.LogInformation("[TRACE] Python client is already running");
                return;
            }

            LogFromPython("[TRACE] Starting Python background thread");
            _pythonThread = new Thread(RunPythonClient)
            {
                IsBackground = true
            };
            _pythonThread.Start();
            LogFromPython("[TRACE] Python client running in background. Press ENTER to stop...");
        }

        public void StopPythonClient()
        {
            LogFromPython("[TRACE] Stoping Python client");
            try
            {
                _clientInstance?.stop();
            }
            finally
            {
                _clientInstance = null;
            }
        }

        private void RunPythonClient()
        {
            try
            {
                string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
                LogFromPython($"[TRACE] Assembly directory: {assemblyDirectory}");

                EnsurePythonRuntimeInitialized();

                // Initialisierung und Start des Python-Clients im GIL-Kontext
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");

                    string version = sys.version.ToString();

                    LogFromPython($"Python Verson: {version}");

                    int major = sys.version_info.major;
                    int minor = sys.version_info.minor;

                    // Add venv site-packages to sys.path
                    var venvPath = $"/opt/venv/lib/python{major}.{minor}/site-packages";
                    LogFromPython($"Packages venv path: {venvPath}");
                    if (File.Exists(venvPath))
                    {
                        LogFromPython($"sys.path.append: {venvPath}");
                        sys.path.append(venvPath);
                    }

                    sys.path.append(Path.Combine(assemblyDirectory, "Python"));

                    LogCallback logCallback = LogFromPython;
                    DumpCallback dumpCallback = DumpFromPython;

                    LogFromPython("[TRACE] Importing Python client module");
                    dynamic clientModule = Py.Import("PythonGrowattClient");
                    dynamic ClientClass = clientModule.Client;

                    LogFromPython("[TRACE] Creating instance of Client class");
                    _clientInstance = ClientClass();
                    _clientInstance.set_log_callback(logCallback);
                    _clientInstance.set_dump_callback(dumpCallback);

                    if (GrowattClientOptions is not null)
                    {
                        LogFromPython("[TRACE] Set options");
                        _clientInstance.set_options(GrowattClientOptions);
                    }

                    LogFromPython("[TRACE] Starting Python client");
                    _clientInstance.start();
                }
            }
            catch (Exception ex)
            {
                LogFromPython("[TRACE] Exception in Python thread: " + ex);
            }
        }

        private void EnsurePythonRuntimeInitialized()
        {
            lock (pythonRuntimeLock)
            {
                if (PythonEngine.IsInitialized)
                {
                    return;
                }

                LogFromPython("[TRACE] Initializing Python runtime");
                PythonEngine.Initialize();
            }
        }

        public void SetSmartPower(DeviceList device, int value)
        {
            if (_clientInstance != null && device.DeviceSn != null)
            {
                ushort startRegister = 310;
                ushort[] values = { 0, (ushort)value, 1 };

                byte[] commandPayload = ModbusCodec.BuildSetMultipleRegistersCommand(startRegister, values);

                _clientInstance.send_msg($"s/33/{device.DeviceSn}", commandPayload, 0, 0);
            }
        }

        public void SetDefaultPower(DeviceList device, int value)
        {
            if (_clientInstance != null && device.DeviceSn != null)
            {
                ushort startRegister = 252;

                byte[] commandPayload = ModbusCodec.BuildSetSingleRegisterCommand(startRegister, (ushort)value);

                _clientInstance.send_msg($"s/33/{device.DeviceSn}", commandPayload, 0, 0);
            }
        }

        /// <summary>
        /// Forwards a Noah time segment configuration to the Python client.
        /// Builds ModbusCodec register values from the query properties and sends them via MQTT.
        /// Sends two commands: PRESET_MULTIPLE_REGISTER for time/power data and PRESET_SINGLE_REGISTER for repeat pattern.
        /// </summary>
        /// <param name="query">The Noah time segment query object (DeviceNoahSetTimeSegmentQuery).</param>
        public void SetNoahTimeSegment(DeviceNoahSetTimeSegmentQuery query)
        {
            if (_clientInstance != null && query.DeviceSn != null)
            {
                var deviceId = query.DeviceSn;

                // Build PRESET_MULTIPLE_REGISTER command (registers 254-260)
                var values = new List<ushort>();

                // Reg 254: Padding
                values.Add(0x00FE);

                // Reg 255: Fixed value
                values.Add(0x0102);

                // Reg 256: Enable (0: off, 1: on)
                if (ushort.TryParse(query.Enable, out ushort enable))
                {
                    values.Add(enable);
                }
                else
                {
                    values.Add(0);
                }

                // Reg 257: Start time (HH:MM format encoded as HH * 256 + MM)
                ushort startTimeValue = 0;
                if (!string.IsNullOrEmpty(query.StartTime) && query.StartTime.Contains(':'))
                {
                    var timeParts = query.StartTime.Split(':');
                    if (ushort.TryParse(timeParts[0], out ushort startHour) &&
                        ushort.TryParse(timeParts[1], out ushort startMinute))
                    {
                        startTimeValue = (ushort)((startHour << 8) | startMinute);
                    }
                }
                values.Add(startTimeValue);

                // Reg 258: End time (HH:MM format encoded as HH * 256 + MM)
                ushort endTimeValue = 0;
                if (!string.IsNullOrEmpty(query.EndTime) && query.EndTime.Contains(':'))
                {
                    var timeParts = query.EndTime.Split(':');
                    if (ushort.TryParse(timeParts[0], out ushort endHour) &&
                        ushort.TryParse(timeParts[1], out ushort endMinute))
                    {
                        endTimeValue = (ushort)((endHour << 8) | endMinute);
                    }
                }
                values.Add(endTimeValue);

                // Reg 259: Power (0-800W)
                ushort powerValue = 0;
                if (ushort.TryParse(query.Power, out ushort power))
                {
                    powerValue = power;
                }
                values.Add(powerValue);

                // Reg 260: Type (1-9: time period identifier)
                if (ushort.TryParse(query.Type, out ushort type))
                {
                    values.Add(type);
                }
                else
                {
                    values.Add(0);
                }

                if (values.Count == 7)
                {
                    byte[] multipleRegistersPayload = ModbusCodec.BuildSetMultipleRegistersCommand(
                        254,
                        values.ToArray()
                    );

                    _clientInstance.send_msg($"s/33/{deviceId}", multipleRegistersPayload, 0, 0);

                    Logger.LogInformation(
                        "[TRACE] SetNoahTimeSegment PRESET_MULTIPLE_REGISTER: type={Type}, enable={Enable}, startTime={StartTime}, endTime={EndTime}, power={Power}",
                        query.Type, query.Enable, query.StartTime, query.EndTime, query.Power
                    );
                }

                // Build PRESET_SINGLE_REGISTER command (register 342 + (slot * 2) for repeat pattern)
                // Slot 1 (Type=1): Register 342 (0x0156)
                // Slot 2 (Type=2): Register 344 (0x0158)
                // Slot 3 (Type=3): Register 346 (0x015A), etc.
                ushort repeatBitmask = ConvertRepeatToBitmask(query.Repeat);
                ushort repeatRegister = 342;
                if (ushort.TryParse(query.Type, out ushort slot) && slot >= 1 && slot <= 9)
                {
                    repeatRegister = (ushort)(342 + ((slot - 1) * 2));
                }
                byte[] singleRegisterPayload = ModbusCodec.BuildSetSingleRegisterCommand(
                    repeatRegister,
                    repeatBitmask
                );

                _clientInstance.send_msg($"s/33/{deviceId}", singleRegisterPayload, 0, 0);

                Logger.LogInformation(
                    "[TRACE] SetNoahTimeSegment PRESET_SINGLE_REGISTER: repeat={Repeat} -> bitmask=0x{Bitmask:X2}, register=0x{Register:X4}",
                    query.Repeat, repeatBitmask, repeatRegister
                );
            }
        }

        /// <summary>
        /// Converts repeat day string (e.g., "1,2,3" or "4,5,6") to a bitmask for register 342.
        /// Days: 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday, 7=Sunday
        /// Bitmask: Bit N-1 represents day N (e.g., "1,2,3" = 0b00000111 = 0x07)
        /// </summary>
        private static ushort ConvertRepeatToBitmask(string? repeatStr)
        {
            if (string.IsNullOrWhiteSpace(repeatStr))
            {
                return 0;
            }

            ushort bitmask = 0;

            // Handle comma-separated day numbers
            if (repeatStr.Contains(','))
            {
                var dayStrings = repeatStr.Split(',');
                foreach (var dayStr in dayStrings)
                {
                    if (ushort.TryParse(dayStr.Trim(), out ushort day) && day >= 1 && day <= 7)
                    {
                        bitmask |= (ushort)(1 << (day - 1));
                    }
                }
            }
            else if (ushort.TryParse(repeatStr, out ushort singleDay) && singleDay >= 1 && singleDay <= 7)
            {
                // Single day
                bitmask = (ushort)(1 << (singleDay - 1));
            }

            return bitmask;
        }

         private Lock logLock = new();

        public void LogFromPython(string message)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    lock (logLock)
                        Logger.LogInformation("{Message}", message);
                }
                catch (Exception)
                {
                    throw;
                }
            });
        }

        private Lock dumpLock = new();

        public void DumpFromPython(string topic, byte[] payload, int qos, int retain, int state, int dup, int mid)
        {
            _ = Task.Run(() =>
            {
                lock (dumpLock)
                {
                    try
                    {
                        PythonMqttMessage message = new PythonMqttMessage
                        {
                            Topic = topic,
                            Payload = payload,
                            Qos = qos,
                            Retain = retain,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            State = state,
                            Dup = dup,
                            Mid = mid
                        };

                        string dumpDirectory = Path.Combine(AppContext.BaseDirectory, "dump");
                        if (!Directory.Exists(dumpDirectory))
                        {
                            Directory.CreateDirectory(dumpDirectory);
                            Logger.LogInformation("[TRACE] Created dump directory at {DumpDirectory}", dumpDirectory);
                        }

                        DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds((long)message.Timestamp).DateTime;
                        string datePart = dateTime.ToString("yyyyMMdd");
                        string topicPart = string.Join("_", message.Topic.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                        string fileName = $"{datePart}_{topicPart}.txt";
                        string filePath = Path.Combine(dumpDirectory, fileName);

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Timestamp: {message.Timestamp}");
                        sb.AppendLine($"DateTime: {dateTime:O}");
                        sb.AppendLine($"Topic: {message.Topic}");
                        sb.AppendLine($"Payload: {BitConverter.ToString(message.Payload)}");
                        sb.AppendLine($"Qos: {message.Qos}");
                        sb.AppendLine($"Retain: {message.Retain}");
                        sb.AppendLine($"Mid: {message.Mid}");
                        sb.AppendLine($"State: {message.State}");
                        sb.AppendLine($"Dup: {message.Dup}");

                        var modBusMessage = ModbusCodec.ParseModbusMessageFromMqtt(message.Payload, message.Topic);

                        if (modBusMessage != null)
                        {
                            sb.AppendLine("Parsed Modbus Message:");
                            sb.AppendLine($"RawDate: {BitConverter.ToString(modBusMessage.DataRaw)}");
                            sb.AppendLine($"Function Code: {modBusMessage.DataHeaderFunction.ToString()}");
                            sb.AppendLine($"Device ID: {modBusMessage.DeviceId}");
                            sb.AppendLine("RegisterBlocks:");
                            foreach (var kvp in modBusMessage.RegisterBlocks)
                            {
                                sb.AppendLine($"Start {kvp.Start}: End {kvp.End} : Values {BitConverter.ToString(kvp.Values)}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("No Modbus message parsed from payload.");
                        }

                        File.AppendAllText(filePath, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[TRACE] Error dumping MQTT message: {Exception}", ex);
                    }
                }
            });
        }
    }
}