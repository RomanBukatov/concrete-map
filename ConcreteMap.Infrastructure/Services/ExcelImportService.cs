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
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[0];
                int count = 0;

                for (int row = 2; worksheet.Cells[row, 1].Value != null; row++)
                {
                    var factory = new Factory
                    {
                        IsVip = worksheet.Cells[row, 1].Text == "Да",
                        Name = worksheet.Cells[row, 2].Text,
                        Phone = worksheet.Cells[row, 3].Text,
                        Address = worksheet.Cells[row, 4].Text,
                        ProductCategories = worksheet.Cells[row, 5].Text,
                        Comment = worksheet.Cells[row, 6].Text,
                        PriceUrl = worksheet.Cells[row, 7].Text,
                        Latitude = ParseDouble(worksheet.Cells[row, 8].Text),
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
                throw new Exception($"Ошибка при импорте данных из Excel: {ex.Message}");
            }
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            // Заменить запятую на точку для парсинга
            value = value.Replace(',', '.');
            return double.TryParse(value, out var result) ? result : 0;
        }
    }
}