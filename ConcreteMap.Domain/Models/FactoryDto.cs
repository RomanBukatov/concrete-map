namespace ConcreteMap.Domain.Models;

public class FactoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVip { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? ProductCategories { get; set; }
    public string? PriceUrl { get; set; }
}