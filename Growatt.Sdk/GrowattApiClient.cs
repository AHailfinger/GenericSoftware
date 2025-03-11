using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Growatt.OSS
{
    public class GrowattApiClient
    {
        private readonly HttpClient _httpClient;

        public GrowattApiClient(string baseAddress, string token)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
            _httpClient.DefaultRequestHeaders.Add("token", token);
        }

        public async Task<string> GetDataAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<Device>> GetDeviceListAsync(int page = 1)
        {
            var endpoint = "/v4/new-api/queryDeviceList";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("page", page.ToString())
            });

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var deviceListResponse = JsonConvert.DeserializeObject<DeviceListResponse>(responseString);

            if (deviceListResponse.Code == 0)
            {
                return deviceListResponse.Data.Devices;
            }
            else
            {
                throw new Exception($"API error: {deviceListResponse.Message}");
            }
        }

        public async Task<List<DeviceNoahInfo>> GetDeviceInfoAsync(string deviceType, string deviceSn)
        {
            var endpoint = "/v4/new-api/queryDeviceInfo";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("deviceType", deviceType),
                new KeyValuePair<string, string>("deviceSn", deviceSn)
            });

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var deviceInfoResponse = JsonConvert.DeserializeObject<DeviceInfoResponse>(responseString);

            if (deviceInfoResponse?.Code == 0)
            {
                return deviceInfoResponse.Data.Noah;
            }
            else
            {
                throw new Exception($"API error: {deviceInfoResponse?.Message}");
            }
        }

        public async Task<List<DeviceNoahLastData>> GetDeviceLastDataAsync(string deviceType, string deviceSn)
        {
            var endpoint = "/v4/new-api/queryLastData";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("deviceType", deviceType),
                new KeyValuePair<string, string>("deviceSn", deviceSn)
            });

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var deviceLastDataResponse = JsonConvert.DeserializeObject<DeviceNoahLastDataResponse>(responseString);

            if (deviceLastDataResponse.Code == 0)
            {
                return deviceLastDataResponse.Data.Noah;
            }
            else
            {
                throw new Exception($"API error: {deviceLastDataResponse.Message}");
            }
        }

        public async Task<List<DeviceNoahHistoricalData>> GetDevicesHistoricalDataAsync(string deviceSn, string deviceType, string date)
        {
            var endpoint = "/v4/new-api/queryDevicesHistoricalData";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("deviceSn", deviceSn),
                new KeyValuePair<string, string>("deviceType", deviceType),
                new KeyValuePair<string, string>("date", date)
            });

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var historicalDataResponse = JsonConvert.DeserializeObject<HistoricalDataResponse>(responseString);

            if (historicalDataResponse.Code == 0)
            {
                return historicalDataResponse.Data.Datas;
            }
            else
            {
                throw new Exception($"API error: {historicalDataResponse.Message}");
            }
        }

        public async Task SetTimeSegmentAsync(DeviceTimeSegment deviceNoahTimeSegment)
        {
            var endpoint = "/v4/new-api/setTimeSegment";
            var content = deviceNoahTimeSegment.ToFormUrlEncodedContent();

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseString);

            if (result.Code != 0)
            {
                throw new Exception($"API error: {result.Message}");
            }
        }

        public async Task SetPowerAsync(string deviceSn, string deviceType, int value)
        {
            var endpoint = "/v4/new-api/setPower";
            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("deviceSn", deviceSn),
            new KeyValuePair<string, string>("deviceType", deviceType),
            new KeyValuePair<string, string>("value", value.ToString())
            });

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseString);

            if (result.Code != 0)
            {
                throw new Exception($"API error: {result.Message}");
            }
        }
    }
}
