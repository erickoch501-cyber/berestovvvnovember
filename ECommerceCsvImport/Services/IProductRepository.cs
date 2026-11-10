using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Хранилище товаров в памяти приложения.
/// </summary>
public interface IProductRepository
{
    void Add(Product product);
    IReadOnlyList<Product> GetAll();
    Product? GetBySku(string sku);
    int Count { get; }
    void Clear();
}
