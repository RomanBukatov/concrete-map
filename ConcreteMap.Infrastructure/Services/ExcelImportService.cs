using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using ConcreteMap.Domain.Entities;
using ConcreteMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConcreteMap.Infrastructure.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(ApplicationDbContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ImportFactoriesAsync(Stream fileStream)
        {
            _logger.LogInformation("Начало импорта Excel файла");
            try
            {
                // 1. Попытка открыть Excel (Валидация формата файла)
                using var package = new ExcelPackage(fileStream);
                if (package.Workbook.Worksheets.Count == 0)
                    throw new Exception("Файл пустой или не является корректным Excel (.xlsx)");

                var worksheet = package.Workbook.Worksheets[0];

                // 2. ВАЛИДАЦИЯ ЗАГОЛОВКОВ (Валидация структуры)
                var headerName = worksheet.Cells[1, 3].Text;   // C
                var headerLat = worksheet.Cells[1, 12].Text;   // L

                if (!headerName.Contains("Наименование", StringComparison.OrdinalIgnoreCase) ||
                    !headerLat.Contains("Latitude", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Неверная структура файла! Проверьте заголовки: Колонка C должна быть 'Наименование', L - 'Latitude'.");
                }

                // 3. ОЧИСТКА БАЗЫ (Только если проверки прошли успешно!)
                // Используем TRUNCATE с CASCADE, чтобы очистить и заводы, и связанные продукты, и сбросить ID
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Factories\" RESTART IDENTITY CASCADE;");

                // 4. ИМПОРТ НОВЫХ ДАННЫХ
                int count = 0;
                int lastRow = worksheet.Dimension?.End.Row ?? 0;

                for (int row = 2; row <= lastRow; row++)
                {
                    var name = worksheet.Cells[row, 3].Text?.Trim();
                    // Пропускаем пустые строки
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var factory = new Factory
                    {
                        IsVip = worksheet.Cells[row, 2].Text?.Trim().Equals("Да", StringComparison.OrdinalIgnoreCase) ?? false,
                        Name = name,
                        Phone = worksheet.Cells[row, 4].Text?.Trim(),
                        Address = worksheet.Cells[row, 5].Text?.Trim(),
                        ProductCategories = worksheet.Cells[row, 6].Text?.Trim(),
                        VipProducts = worksheet.Cells[row, 7].Text?.Trim(),
                        Comment = worksheet.Cells[row, 8].Text?.Trim(),
                        PriceUrl = CleanUrl(worksheet.Cells[row, 10].Text),
                        Latitude = ParseDouble(worksheet.Cells[row, 12].Text),
                        Longitude = ParseDouble(worksheet.Cells[row, 13].Text)
                    };

                    _context.Factories.Add(factory);
                    count++;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Импорт завершен. Добавлено {Count} записей", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при импорте");
                // Если это наша ошибка валидации - прокидываем сообщение
                if (ex.Message.Contains("Неверная структура") || ex.Message.Contains("Файл пустой")) 
                    throw;
                
                // Если EPPlus упал при открытии файла
                if (ex is InvalidDataException || ex.Message.Contains("Corrupt"))
                    throw new Exception("Файл поврежден или не является форматом .xlsx");

                // Остальные ошибки
                throw new Exception($"Системная ошибка импорта: {ex.Message}");
            }
        }

        private string? CleanUrl(string? rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return null;
            var url = rawUrl.Trim();
            var garbage = new[] { "нет", "-", "нету", "no", "none", "n/a", "нет пока" };
            if (garbage.Contains(url.ToLower())) return null;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return "https://" + url;
            return url;
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var normalized = value.Replace(',', '.').Trim();
            return double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        public string ExtractAllText(Stream fileStream)
        {
            try
            {
                // Не используем using, чтобы не закрыть поток, который нужен для S3
                var package = new ExcelPackage(fileStream);
                
                if (package.Workbook.Worksheets.Count == 0) return "";

                var sb = new System.Text.StringBuilder();
                var worksheet = package.Workbook.Worksheets[0];

                // Читаем все ячейки, где есть данные
                var range = worksheet.Cells[worksheet.Dimension.Address];
                
                foreach (var cell in range)
                {
                    var text = cell.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.Append(text).Append(" ");
                    }
                }

                // Сбрасываем позицию потока в начало, это критично для последующей загрузки в S3!
                fileStream.Position = 0;

                return sb.ToString();
            }
            catch (Exception)
            {
                // Если не смогли прочитать (не Excel) - возвращаем пустую строку, не ломаем процесс
                fileStream.Position = 0;
                return "";
            }
        }
    }
}