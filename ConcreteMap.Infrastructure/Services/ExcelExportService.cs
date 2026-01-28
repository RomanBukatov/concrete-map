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

            // --- ЗАГОЛОВКИ (Как в исходном файле клиента) ---
            worksheet.Cells[1, 1].Value = "№";
            worksheet.Cells[1, 2].Value = "Производитель"; // IsVip
            worksheet.Cells[1, 3].Value = "Наименование поставщика"; // ВАЖНО для валидации
            worksheet.Cells[1, 4].Value = "Контактное лицо"; // Phone
            worksheet.Cells[1, 5].Value = "Адрес производства";
            worksheet.Cells[1, 6].Value = "Основная продукция";
            worksheet.Cells[1, 7].Value = "VIP Продукция"; // Пусто
            worksheet.Cells[1, 8].Value = "Вся продукция"; // Comment
            worksheet.Cells[1, 9].Value = "Комментарий"; // Пусто
            worksheet.Cells[1, 10].Value = "Сайт"; // PriceUrl
            worksheet.Cells[1, 11].Value = "Прайс"; // Пусто
            worksheet.Cells[1, 12].Value = "Latitude"; // ВАЖНО для валидации
            worksheet.Cells[1, 13].Value = "Longitude";

            // --- ДАННЫЕ ---
            for (int i = 0; i < factories.Count; i++)
            {
                var f = factories[i];
                int row = i + 2;

                worksheet.Cells[row, 1].Value = i + 1; // ID по порядку
                worksheet.Cells[row, 2].Value = f.IsVip ? "Да" : "Нет"; // Конвертация обратно
                worksheet.Cells[row, 3].Value = f.Name;
                worksheet.Cells[row, 4].Value = f.Phone;
                worksheet.Cells[row, 5].Value = f.Address;
                worksheet.Cells[row, 6].Value = f.ProductCategories;
                worksheet.Cells[row, 7].Value = f.VipProducts;
                worksheet.Cells[row, 8].Value = f.Comment;
                // 9 пропускаем
                worksheet.Cells[row, 10].Value = f.PriceUrl;
                worksheet.Cells[row, 11].Value = f.PriceListUrl;
                worksheet.Cells[row, 12].Value = f.Latitude;
                worksheet.Cells[row, 13].Value = f.Longitude;
            }

            // Автоширина колонок для красоты
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}