using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConcreteMap.Infrastructure.Services;

namespace ConcreteMap.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly ExcelExportService _service;

        public ExportController(ExcelExportService service)
        {
            _service = service;
        }

        [HttpGet("factories")]
        public async Task<IActionResult> ExportFactories()
        {
            var content = await _service.ExportFactoriesAsync();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "factories_export.xlsx");
        }
    }
}