using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Ping.Services.Admin;
using System.Threading.Tasks;

namespace Ping.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/app-version")]
    [Route("api/v{version:apiVersion}/app-version")]
    public class AppVersionController(IMinVersionService minVersionService) : ControllerBase
    {
        /// <summary>
        /// Returns the currently configured minimum supported app version, if any.
        /// Clients below this version should block usage and prompt an update.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMinVersion()
        {
            var config = await minVersionService.GetMinVersionAsync();
            return Ok(new { minVersion = config.MinVersion, message = config.Message });
        }
    }
}
