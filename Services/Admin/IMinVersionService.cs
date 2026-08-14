using System.Threading.Tasks;

namespace Ping.Services.Admin
{
    public record MinVersionConfig(string? MinVersion, string? Message);

    public interface IMinVersionService
    {
        Task<MinVersionConfig> GetMinVersionAsync();
        Task SetMinVersionAsync(string? minVersion, string? message);
    }
}
