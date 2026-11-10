using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Потокобезопасный репозиторий товаров на основе List в памяти.
/// Регистрируется как singleton, чтобы данные сохранялись между запросами.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _products.Count;
            }
        }
    }

    /// <summary>Добавляет товар в коллекцию.</summary>
    public void Add(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        lock (_lock)
        {
            _products.Add(product);
        }
    }

    /// <summary>Возвращает копию списка товаров (снапшот).</summary>
    public IReadOnlyList<Product> GetAll()
    {
        lock (_lock)
        {
            return _products.ToList();
        }
    }

    /// <summary>Ищет товар по артикулу без учета регистра и пробелов по краям.</summary>
    public Product? GetBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        lock (_lock)
        {
            return _products.FirstOrDefault(p =>
                string.Equals(p.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Удаляет все товары из коллекции.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _products.Clear();
        }
    }
}
