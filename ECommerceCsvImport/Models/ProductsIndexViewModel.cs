namespace ECommerceCsvImport.Models;

/// <summary>
/// Модель представления страницы списка товаров.
/// </summary>
public class ProductsIndexViewModel
{
    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
    public int TotalCount => Products.Count;
    public decimal TotalValue => Products.Sum(p => p.Price);
}
