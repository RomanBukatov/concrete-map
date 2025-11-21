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

    public async Task<int> ImportFactoriesAsync(Stream fileStream)
    {
        try
        {
            using var package = new ExcelPackage(fileStream);
            if (package.Workbook.Worksheets.Count == 0)
                throw new Exception("Excel файл не содержит листов.");

            var worksheet = package.Workbook.Worksheets[0];
            int count = 0;

            // МАППИНГ ПО СКРИНШОТУ:
            // A(1) - №
            // B(2) - IsVip
            // C(3) - Name
            // D(4) - Phone
            // E(5) - Address
            // F(6) - ProductCategories (Основная продукция)
            // G(7) - VIP Продукция (пропускаем)
            // H(8) - Вся продукция (Берем в Comment, это полезно для поиска!)
            // I(9) - Комментарий (там часто пусто)
            // J(10) - Сайт (PriceUrl)
            // K(11) - Прайс
            // L(12) - Latitude
            // M(13) - Longitude

            for (int row = 2; worksheet.Cells[row, 3].Value != null; row++)
            {
                var factory = new Factory
                {
                    IsVip = worksheet.Cells[row, 2].Text?.Trim().Equals("Да", StringComparison.OrdinalIgnoreCase) ?? false,
                    Name = worksheet.Cells[row, 3].Text?.Trim() ?? "Без названия",
                    Phone = worksheet.Cells[row, 4].Text?.Trim(),
                    Address = worksheet.Cells[row, 5].Text?.Trim(),
                    ProductCategories = worksheet.Cells[row, 6].Text?.Trim(),
                    
                    // Берем "Вся продукция" (H=8), так как там полный список для клиента
                    Comment = worksheet.Cells[row, 8].Text?.Trim(), 
                    
                    // Сайт (J=10)
                    PriceUrl = worksheet.Cells[row, 10].Text?.Trim(),
                    
                    // Координаты (L=12, M=13)
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