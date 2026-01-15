using System.Collections.Generic;

namespace ConcreteMap.Domain.Entities
{
    public class Factory
    {
        public int Id { get; set; }

        // Основные данные
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; } // Адрес производства
        public string? Phone { get; set; }   // Контактные телефоны
        public string? PriceUrl { get; set; } // Ссылка на прайс

        // Данные для карты
        public double Latitude { get; set; }  // Широта
        public double Longitude { get; set; } // Долгота
        public bool IsVip { get; set; }       // Флаг: красный/большой пин

        // Описательные поля (из Excel)
        public string? ProductCategories { get; set; } // "Основная продукция" (строка из экселя)
        public string? Comment { get; set; }           // Комментарий/Ассортимент
        public string? VipProducts { get; set; }

        // Прайс-лист
        public string? PriceListUrl { get; set; }      // Ссылка на файл в S3
        public string? PriceListContent { get; set; } // Текст из файла для поиска

        // Навигационное свойство (связь 1-ко-многим)
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}