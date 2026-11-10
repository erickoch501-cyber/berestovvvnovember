using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceCsvImport.Controllers;

/// <summary>
/// Контроллер товаров. Бизнес-логика импорта находится в сервисе, контроллер только связывает его с представлениями.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>Страница со списком товаров из памяти.</summary>
    [HttpGet]
    public ActionResult Index()
    {
        var viewModel = new ProductsIndexViewModel
        {
            Products = _repository.GetAll()
        };

        return View(viewModel);
    }

    /// <summary>Страница с формой импорта.</summary>
    [HttpGet]
    public ActionResult Import()
    {
        return View(new ImportViewModel());
    }

    /// <summary>Запуск импорта по указанному пути и вывод результата.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Import(string? filePath)
    {
        var resolvedPath = ResolveFilePath(filePath);

        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = new ImportResult
                {
                    Errors =
                    {
                        new ImportError
                        {
                            RowNumber = 0,
                            Message = "Укажите путь к CSV-файлу"
                        }
                    }
                }
            });
        }

        var result = _importService.ImportFromCsv(resolvedPath);

        return View("Import", new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    /// <summary>
    /// Преобразует введенный путь: "/csv/file.csv" ищется в wwwroot, полный путь (C:\data\file.csv) используется как есть.
    /// </summary>
    private string? ResolveFilePath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        if (trimmed.StartsWith('/'))
            return Path.Combine(_environment.WebRootPath, trimmed.TrimStart('/'));

        if (Path.IsPathRooted(trimmed))
            return trimmed;

        return Path.Combine(_environment.WebRootPath, trimmed);
    }
}
