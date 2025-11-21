using System;
using System.IO;
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

        // Статический конструктор УДАЛЕН, так как лицензия теперь в appsettings.json

        public async Task<int> ImportFactoriesAsync(Stream fileStream)
        {
            try
            {
                using var package = new ExcelPackage(fileStream);
                
                if (package.Workbook.Worksheets.Count == 0)
                    throw new Exception("Excel файл не содержит листов.");

                var worksheet = package.Workbook.Worksheets[0];
                int count = 0;

                // Начинаем со 2-й строки
                for (int row = 2; worksheet.Cells[row, 1].Value != null; row++)
                {
                    var factory = new Factory
                    {
                        // Col 1: IsVip
                        IsVip = worksheet.Cells[row, 1].Text?.Trim().Equals("Да", StringComparison.OrdinalIgnoreCase) ?? false,
                        // Col 2: Name
                        Name = worksheet.Cells[row, 2].Text?.Trim() ?? "Без названия",
                        // Col 3: Phone
                        Phone = worksheet.Cells[row, 3].Text?.Trim(),
                        // Col 4: Address
                        Address = worksheet.Cells[row, 4].Text?.Trim(),
                        // Col 5: ProductCategories
                        ProductCategories = worksheet.Cells[row, 5].Text?.Trim(),
                        // Col 6: Comment
                        Comment = worksheet.Cells[row, 6].Text?.Trim(),
                        // Col 7: PriceUrl
                        PriceUrl = worksheet.Cells[row, 7].Text?.Trim(),
                        // Col 8: Latitude
                        Latitude = ParseDouble(worksheet.Cells[row, 8].Text),
                        // Col 9: Longitude
                        Longitude = ParseDouble(worksheet.Cells[row, 9].Text)
                    };
                    
                    _context.Factories.Add(factory);
                    count++;
                }

                await _context.SaveChangesAsync();
                return count;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка импорта: {ex.Message}", ex);
            }
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var normalized = value.Replace(',', '.').Trim();
            return double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
    }
}