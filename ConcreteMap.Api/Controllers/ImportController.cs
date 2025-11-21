using Microsoft.AspNetCore.Mvc;
using ConcreteMap.Infrastructure.Services;

namespace ConcreteMap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ExcelImportService _service;

        public ImportController(ExcelImportService service)
        {
            _service = service;
        }

        [HttpPost("factories")]
        public async Task<IActionResult> ImportFactories(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не выбран");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _service.ImportFactoriesAsync(stream);
                return Ok(new { count = result, message = "Импорт успешно выполнен" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}