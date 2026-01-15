using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConcreteMap.Infrastructure.Data;
using ConcreteMap.Infrastructure.Services;
using ConcreteMap.Domain.Entities;
using System.IO;

namespace ConcreteMap.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PriceListController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly S3Service _s3Service;
        private readonly ExcelImportService _excelImportService;

        public PriceListController(ApplicationDbContext context, S3Service s3Service, ExcelImportService excelImportService)
        {
            _context = context;
            _s3Service = s3Service;
            _excelImportService = excelImportService;
        }

        [HttpPost("{factoryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(int factoryId, IFormFile file)
        {
            var factory = await _context.Factories.FindAsync(factoryId);
            if (factory == null)
            {
                return NotFound();
            }

            using var stream = file.OpenReadStream();

            if (!string.IsNullOrEmpty(factory.PriceListUrl))
            {
                await _s3Service.DeleteFileAsync(factory.PriceListUrl);
            }

            var text = _excelImportService.ExtractAllText(stream);
            var url = await _s3Service.UploadFileAsync(stream, file.FileName);

            factory.PriceListUrl = url;
            factory.PriceListContent = text;

            await _context.SaveChangesAsync();

            return Ok(new { url });
        }

        [HttpDelete("{factoryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(int factoryId)
        {
            var factory = await _context.Factories.FindAsync(factoryId);
            if (factory == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(factory.PriceListUrl))
            {
                await _s3Service.DeleteFileAsync(factory.PriceListUrl);
            }

            factory.PriceListUrl = null;
            factory.PriceListContent = null;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("download/{factoryId}")]
        [Authorize] // Доступно всем залогиненным
        public async Task<IActionResult> Download(int factoryId)
        {
            var factory = await _context.Factories.FindAsync(factoryId);
            if (factory == null || string.IsNullOrEmpty(factory.PriceListUrl))
            {
                return NotFound("Прайс-лист не найден");
            }

            var stream = await _s3Service.GetFileStreamAsync(factory.PriceListUrl);
            if (stream == null) return NotFound("Файл отсутствует в хранилище");

            // Генерируем красивое имя файла: "Прайс_НазваниеЗавода.xlsx"
            // Убираем спецсимволы из имени завода, чтобы не сломать заголовок
            var safeName = string.Join("_", factory.Name.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"Price_{safeName}.xlsx";

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}