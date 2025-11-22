using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using ConcreteMap.Domain.Entities;
using ConcreteMap.Infrastructure.Data;

namespace ConcreteMap.Infrastructure.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _context;

        public ExcelImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> ImportFactoriesAsync(Stream fileStream)
        {
            try
            {
                using var package = new ExcelPackage(fileStream);
                if (package.Workbook.Worksheets.Count == 0)
                    throw new Exception("Excel файл не содержит листов.");

                var worksheet = package.Workbook.Worksheets[0];

                // 1. ВАЛИДАЦИЯ ЗАГОЛОВКОВ
                var headerName = worksheet.Cells[1, 3].Text;
                var headerLat = worksheet.Cells[1, 12].Text;

                if (!headerName.Contains("Наименование", StringComparison.OrdinalIgnoreCase) ||
                    !headerLat.Contains("Latitude", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Неверный формат файла! Проверьте, что колонки не сдвинуты. Колонка C должна быть 'Наименование', L - 'Latitude'.");
                }

                // 2. ЧТЕНИЕ ДАННЫХ
                int count = 0;

                // ИСПРАВЛЕНИЕ: Получаем номер последней строки в файле
                int lastRow = worksheet.Dimension?.End.Row ?? 0;

                // Цикл идет строго до последней строки
                for (int row = 2; row <= lastRow; row++)
                {
                    // Получаем имя
                    var name = worksheet.Cells[row, 3].Text?.Trim();

                    // Если имя пустое — это пустая строка или "хвост" объединения. ПРОПУСКАЕМ (continue), но не выходим.
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var factory = new Factory
                    {
                        IsVip = worksheet.Cells[row, 2].Text?.Trim().Equals("Да", StringComparison.OrdinalIgnoreCase) ?? false,
                        Name = name,
                        Phone = worksheet.Cells[row, 4].Text?.Trim(),
                        Address = worksheet.Cells[row, 5].Text?.Trim(),
                        ProductCategories = worksheet.Cells[row, 6].Text?.Trim(),
                        Comment = worksheet.Cells[row, 8].Text?.Trim(),
                        PriceUrl = CleanUrl(worksheet.Cells[row, 10].Text),
                        Latitude = ParseDouble(worksheet.Cells[row, 12].Text),
                        Longitude = ParseDouble(worksheet.Cells[row, 13].Text)
                    };

                    _context.Factories.Add(factory);
                    count++;
                }

                await _context.SaveChangesAsync();
                return count;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Неверный формат")) throw;
                throw new Exception($"Ошибка импорта: {ex.Message}", ex);
            }
        }

        private string? CleanUrl(string? rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return null;

            var url = rawUrl.Trim();
            var garbage = new[] { "нет", "-", "нету", "no", "none", "n/a", "нет пока" };

            if (garbage.Contains(url.ToLower())) return null;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url;
            }
            return url;
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var normalized = value.Replace(',', '.').Trim();
            return double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
    }
}