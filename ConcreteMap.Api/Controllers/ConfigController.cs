using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConcreteMap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("map-key")]
        public IActionResult GetMapKey()
        {
            return Ok(new { key = _configuration["YandexMaps:ApiKey"] });
        }
    }
}