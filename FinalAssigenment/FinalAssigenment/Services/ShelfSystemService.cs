using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using System.ComponentModel.Design;

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

    public List<EquipmentState> eqpStatusList;
    public void UpdateEqpStatus(string eqpName, int statusType)
    {

    }

    public async Task UnloadValidationAsync(RelayCommand unload)
    {
        string url = "";
        if (unload.EqpName == "Eqp01") url = "http://localhost:8090/"; 
        else if (unload.EqpName == "Eqp02") url = "http://localhost:8091/"; 
        else if (unload.EqpName == "Eqp03") url = "http://localhost:8092/";
        
        await PostHttpClientAsync(url, unload);
    }

    public async Task PostHttpClientAsync(string url, RelayCommand sendCommand)
    {

    }
}
