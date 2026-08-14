using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ping.Services.Admin
{
    public class MinVersionService : IMinVersionService
    {
        private readonly string _filePath;
        private MinVersionData? _cached;
        private bool _isCached = false;
        private readonly object _lock = new object();

        public MinVersionService(string? filePath = null)
        {
            _filePath = filePath ?? Path.Combine(Directory.GetCurrentDirectory(), "minversion.json");
        }

        public async Task<MinVersionConfig> GetMinVersionAsync()
        {
            if (_isCached)
            {
                return new MinVersionConfig(_cached?.MinVersion, _cached?.Message);
            }

            if (!File.Exists(_filePath))
            {
                lock (_lock)
                {
                    _cached = null;
                    _isCached = true;
                }
                return new MinVersionConfig(null, null);
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var data = JsonSerializer.Deserialize<MinVersionData>(json);
                lock (_lock)
                {
                    _cached = data;
                    _isCached = true;
                }
                return new MinVersionConfig(data?.MinVersion, data?.Message);
            }
            catch
            {
                return new MinVersionConfig(null, null);
            }
        }

        public async Task SetMinVersionAsync(string? minVersion, string? message)
        {
            var data = new MinVersionData { MinVersion = minVersion, Message = message };
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(_filePath, json);

            lock (_lock)
            {
                _cached = data;
                _isCached = true;
            }
        }

        private class MinVersionData
        {
            public string? MinVersion { get; set; }
            public string? Message { get; set; }
        }
    }
}
