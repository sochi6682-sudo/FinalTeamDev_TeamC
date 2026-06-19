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

    public List<EquipmentState> eqpStatusList = [
            new(){EqpName = "EQP01", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP02", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP03", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0}
        ];
    public void UpdateEqpStatus(string eqpName, int statusType)
    {

    }

    public async Task UnloadValidationAsync(RelayCommand unload)
    {   
        await PostHttpClientAsync(unload.EqpName, unload);
    }

    public async Task PostHttpClientAsync(string eqpName, RelayCommand sendCommand)
    {
        string url = "";
        if (eqpName == "Eqp01") url = "http://localhost:8090/";
        else if (eqpName == "Eqp02") url = "http://localhost:8091/";
        else if (eqpName == "Eqp03") url = "http://localhost:8092/";
    }
}
