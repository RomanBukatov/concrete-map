using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using ConcreteMap.Domain.Entities;
using ConcreteMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConcreteMap.Infrastructure.Services
{
    public class ExcelExportService
    {
        private readonly ApplicationDbContext _context;

        public ExcelExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportFactoriesAsync()
        {
            var factories = await _context.Factories.AsNoTracking().ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Factories");

            // Заголовки
            worksheet.Cells[1, 1].Value = "IsVip";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Phone";
            worksheet.Cells[1, 4].Value = "Address";
            worksheet.Cells[1, 5].Value = "ProductCategories";
            worksheet.Cells[1, 6].Value = "Comment";
            worksheet.Cells[1, 7].Value = "PriceUrl";
            worksheet.Cells[1, 8].Value = "Latitude";
            worksheet.Cells[1, 9].Value = "Longitude";

            // Данные
            for (int i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = factory.IsVip;
                worksheet.Cells[row, 2].Value = factory.Name;
                worksheet.Cells[row, 3].Value = factory.Phone;
                worksheet.Cells[row, 4].Value = factory.Address;
                worksheet.Cells[row, 5].Value = factory.ProductCategories;
                worksheet.Cells[row, 6].Value = factory.Comment;
                worksheet.Cells[row, 7].Value = factory.PriceUrl;
                worksheet.Cells[row, 8].Value = factory.Latitude;
                worksheet.Cells[row, 9].Value = factory.Longitude;
            }

            return package.GetAsByteArray();
        }
    }
}