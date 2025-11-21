namespace ConcreteMap.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty; // Маркировка (напр. "2П30-18-30")

        // Используем decimal для денег, это стандарт
        public decimal Price { get; set; }

        // Внешний ключ (Foreign Key)
        public int FactoryId { get; set; }

        // Навигационное свойство
        public Factory? Factory { get; set; }
    }
}