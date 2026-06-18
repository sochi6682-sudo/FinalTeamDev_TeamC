using FinalAssigenment.Repositories;

namespace FinalAssigenment.Services;

public class ShelfSystemService
{
    private readonly ILogger<ShelfSystemService> _logger;
    private readonly SqlRepository _repository;
    private readonly HttpClient _httpClient;
    public ShelfSystemService(
        ILogger<ShelfSystemService> logger,
        SqlRepository repository,
        HttpClient httpClient)
    {
        _logger = logger;
        _repository = repository;
        _httpClient = httpClient;
    }
}
