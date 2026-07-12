using EnergyAutomate.Definitions;
using Newtonsoft.Json;

namespace EnergyAutomate.Watchdogs
{
    public class ApiQueueWatchdog<T> where T : class, IDeviceQuery
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        #region Public Constructors

        public ApiQueueWatchdog(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        #endregion Public Constructors

        #region Events

        //Action delegate T item
        public event Func<T, GrowattApiClient, ILogger, Task<ApiException?>>? OnItemDequeued;

        #endregion Events

        #region Properties

        private List<T> Collection { get; set; } = new();

        public int Count => Collection.Count;

        private bool IsProceeding { get; set; }

        private ILogger<ApiQueueWatchdog<T>> Logger => ServiceProvider.GetRequiredService<ILogger<ApiQueueWatchdog<T>>>();

        private IServiceProvider ServiceProvider { get; init; }

        private List<ApiCallLog> GrowattDataReads { get; set; } = [];

        public Dictionary<string, int> ApiSettingDataReadsDelaySec { get; set; } = new Dictionary<string, int>() {
            { nameof(DeviceMinInfoDataQuery), 60 * 5 } ,
            { nameof(DeviceMinLastDataQuery), 60 * 5 } ,
            { nameof(DeviceNoahInfoDataQuery), 60 * 5 } ,
            { nameof(DeviceNoahLastDataQuery), 61 } ,
            { nameof(DeviceNoahSetLowLimitSocQuery), 5 } ,
            { nameof(DeviceNoahSetPowerQuery), 5 } ,
            { nameof(DeviceNoahSetTimeSegmentQuery), 5 } ,
            { nameof(DeviceListQuery), 60 * 5 + 1 }
        };

        #endregion Properties

        #region Public Methods

        public async Task<int> CountAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return Collection.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                Collection.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T?> DequeueAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (Collection.Count != 0)
                {
                    var item = Collection[0];
                    Collection.RemoveAt(0);
                    return item;
                }
                else
                    return default;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task EnqueueAsync(T item)
        {
            await _semaphore.WaitAsync();
            try
            {
                Collection.Add(item);
            }
            finally
            {
                _semaphore.Release();
            }

            _ = Task.Run(Proceeding).ContinueWith(task =>
            {
                Logger.LogError(task.Exception, "ApiQueueWatchdog<{Type}> processing failed", typeof(T).Name);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task EnqueueAsync(Queue<T> items)
        {
            await _semaphore.WaitAsync();
            try
            {
                while (items.Count > 0)
                {
                    var item = items.Dequeue();
                    Collection.Add(item);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            _ = Task.Run(Proceeding).ContinueWith(task =>
            {
                Logger.LogError(task.Exception, "ApiQueueWatchdog<{Type}> processing failed", typeof(T).Name);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task Proceeding()
        {
            if (IsProceeding)
                return;

            IsProceeding = true;

            do
            {
                if (await CountAsync() == 0)
                    break;

                var item = await DequeueAsync();

                if (item == null)
                    break;

                var queryType = item.GetType().Name;
                var delay = ApiSettingDataReadsDelaySec[queryType];

                var isNotLocked = !GrowattDataReads.Where(x => x.MethodeName == queryType && x.TS.UtcDateTime >= DateTimeOffset.UtcNow.AddSeconds(-delay)).Any();

                if (item != null && isNotLocked)
                {
                    var ts = DateTimeOffset.UtcNow;
                    Logger.LogTrace("ApiQueueWatchdog<{Type}>: {queryType} - {delay} sec >> {TS}", typeof(T).Name, queryType, delay, ts);
                    var entry = new ApiCallLog()
                    {
                        MethodeName = queryType,
                        TS = ts,
                        RaisedError = false
                    };

                    //Add log entry
                    GrowattDataReads.Add(entry);

                    var handler = OnItemDequeued;
                    if (handler != null)
                    {
                        var apiException = await handler.Invoke(item, ServiceProvider.GetRequiredService<GrowattApiClient>(), Logger);
                        if (apiException != default)
                        {
                            Logger.LogError("ApiQueueWatchdog<{Type}> ErrorCode: {ErrorCode} Message: {Message}",
                                item.GetType().Name, apiException.ErrorCode, apiException.Message);
                            Logger.LogError("Failed item payload: {Item}", JsonConvert.SerializeObject(item));
                        }
                    }
                }
                else if (item != null && item.Force)
                {
                    await EnqueueAsync(item);
                }

            } while (await CountAsync() > 0);

            IsProceeding = false;
        }

        #endregion Public Methods
    }
}
