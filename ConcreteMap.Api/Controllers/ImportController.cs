using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Roles = "Admin")]
        [HttpPost("factories")]
        public async Task<IActionResult> ImportFactories(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Файл не выбран" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _service.ImportFactoriesAsync(stream);
                return Ok(new { count = result, message = "Импорт успешно выполнен" });
            }
            catch (Exception ex)
            {
                // ВАЖНО: Возвращаем ошибку как JSON объект, чтобы JS мог её прочитать
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}