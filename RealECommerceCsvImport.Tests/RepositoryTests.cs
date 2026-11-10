using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

/// <summary>
/// Тесты InMemoryProductRepository.
/// </summary>
public class RepositoryTests
{
    private static Product CreateProduct(string sku = "1001")
    {
        return new Product
        {
            Sku = sku,
            Name = "Ноутбук Lenovo IdeaPad 3",
            Price = 45990m,
            Brand = "Lenovo"
        };
    }

    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repository = new InMemoryProductRepository();
        var product = CreateProduct();

        repository.Add(product);

        var all = repository.GetAll();
        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public void GetBySku_ExistingSku_ReturnsProduct()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("1001"));

        var found = repository.GetBySku("1001");

        Assert.NotNull(found);
        Assert.Equal("1001", found!.Sku);
    }

    [Fact]
    public void GetBySku_IgnoresCaseAndSurroundingSpaces()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(CreateProduct("AB-1001"));

        Assert.NotNull(repository.GetBySku(" AB-1001 "));
        Assert.NotNull(repository.GetBySku("ab-1001"));
    }

    [Fact]
    public void EmptyRepository_HasNoProducts()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
        Assert.Null(repository.GetBySku("1001"));
    }
}
