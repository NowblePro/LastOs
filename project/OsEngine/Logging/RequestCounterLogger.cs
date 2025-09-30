using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OsEngine.Logging
{
    public class RequestData
    {
        public string method { get; set; }
        public string endpoint { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class RequestLogger
    {
        private readonly string _serverUrl;
        private readonly HttpClient _httpClient;

        public RequestLogger(string serverUrl = "http://localhost:8085")
        {
            _serverUrl = serverUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public void LogRequest(string method, string endpoint)
        {
            try
            {
                method = method.ToUpper();

                var requestData = new RequestData
                {
                    method = method,
                    endpoint = endpoint,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _ = _httpClient.PostAsync($"{_serverUrl}/api/requests", content);
            }
            catch
            {
            }
        }
    }
}
